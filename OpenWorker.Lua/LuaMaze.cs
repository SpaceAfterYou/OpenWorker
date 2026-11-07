using System.Diagnostics;
using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using Lua;
using OpenWorker.Batch;
using OpenWorker.Batch.Actions;
using OpenWorker.Batch.Entities;
using OpenWorker.Batch.Extensions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.Gameplay;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Hotspot;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Domain.Batch.Enums;
using OpenWorker.Hotspot.Modules.Maze.Responses;
using OpenWorker.Hotspot.Modules.Monster.Responses;
using OpenWorker.Hotspot.Modules.Skill.Requests;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Lua.Managers;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Lua;

public enum MazeState
{
    None = 0x0,
    Run = 0x1,
    StartBoss = 0x2,
    WaitToDie = 0x3,
    ModeFail = 0x4,
    Wait = 0x5
}

[LuaObject]
public partial class LuaMaze(
    LuaState state, 
    World ecs, 
    Entity player, 
    LuaCreatureManager creatureManager,
    BuffManager buffManager,
    QuestManager questManager,
    VBatchFile batch,
    IReadOnlyCollection<InteractionObjectRow> interactions,
    SkillActionProvider actions,
    CombatTables combat
) : ILuaUserData
{
    public LuaState State { get; } = state;
    
    public World Ecs { get; } = ecs;
    public VBatchFile Batch { get; } = batch;

    public MazeProgress Progress { get; } = new(batch, interactions);

    // A monster that counts hits takes them no faster than this.
    private const long CountedHitMilliseconds = 500;

    private static readonly TimeSpan FlightTick = TimeSpan.FromMilliseconds(50);

    private int SkillGeneration { get; set; }
    private uint ProjectileSession { get; set; }
    private bool Started { get; set; }
    private bool Closed { get; set; }

    /// <summary>
    /// The player has left: timers must not touch an entity that is gone, or that belongs to someone else by now.
    /// </summary>
    public void Close()
    {
        Closed = true;

        creatureManager.Close();
    }

    /// <summary>
    /// Spawns what a sector starts with. Boxes that wait for a signal are started by the script.
    /// </summary>
    [LuaIgnoreMember]
    public void SpawnSector(int sector)
    {
        var kills = creatureManager.GetKillProgress(sector);

        kills.MarkCurrentStepSpawned();

        foreach (var box in Batch.EventBox.MonsterSpawns.Where(e => e.Sector == sector && e.CreationCondition == CreationConditionType.Immediate && e.Step == kills.Step + 1))
        {
            creatureManager.SpawnOnce(player, box.Id);
        }
    }

    /// <summary>
    /// The client only says that a skill has started, so the server decides what it hits and when.
    /// A new skill interrupts the previous one, along with the attacks it has not reached yet.
    /// </summary>
    [LuaIgnoreMember]
    public void StartSkill(int skill, SkillPositionValue position)
    {
        if (!Started)
        {
            return;
        }

        var generation = ++SkillGeneration;
        var hero = Ecs.Get<PersonInfoComponent>(player).Hero;

        foreach (var hit in actions.GetHits(skill, hero))
        {
            _ = RunLaterAsync(hit.Time, () =>
            {
                if (generation != SkillGeneration)
                {
                    return ValueTask.CompletedTask;
                }

                return hit.Trigger.IsTrap ? PlaceTrapAsync(skill, hit.Trigger, position) : ApplyHitAsync(skill, hit, position);
            });
        }
    }

    private ValueTask ApplyHitAsync(int skill, SkillHit hit, SkillPositionValue position)
    {
        var attacker = Ecs.Get<WorldComponent>(player);
        var area = new AttackArea(hit.Trigger, attacker.Position, attacker.Rotation);
        var targets = creatureManager.FindMonsters((place, body) => area.Touches(place, body.BodyRadius, body.BodyHeight));

        return ApplyDamageAsync(skill, hit.Trigger, targets, position);
    }

    /// <summary>
    /// A trap outlives the skill that placed it: a new skill does not take it away.
    /// </summary>
    private async ValueTask PlaceTrapAsync(int skill, AttackTrigger trigger, SkillPositionValue position)
    {
        var info = trigger.Projectile!;
        var attacker = Ecs.Get<WorldComponent>(player);
        var area = new AttackArea(trigger, attacker.Position, attacker.Rotation);
        var (sin, cos) = MathF.SinCos(attacker.Rotation * MathF.PI / 180f);

        if (!actions.IsFlownByClient(skill))
        {
            player.Get<ServerSessionComponent>().Send(new SkillProjectileBroadcastResponse
            {
                Owner = Ecs.Get<ActorComponent>(player),
                Projectile = default(ProjectileShotValue) with
                {
                    Skill = skill,
                    Attack = (short)trigger.Id,
                    Position = attacker.Position,
                    Direction = new Vector3(sin, -cos, 0f),
                    Session = ++ProjectileSession
                }
            });
        }

        var left = info.LifeTime;

        while (!Closed)
        {
            var targets = creatureManager.FindMonsters((place, body) => area.Touches(place, body.BodyRadius, body.BodyHeight));

            await ApplyDamageAsync(skill, trigger, targets, position);

            if (!info.IsContinuous || info.ContinuousTime <= 0f || left <= info.ContinuousTime)
            {
                return;
            }

            left -= info.ContinuousTime;

            await Task.Delay(TimeSpan.FromSeconds(info.ContinuousTime));
        }
    }

    /// <summary>
    /// The client fires a projectile when its animation gets there, and the server follows it to see whom it hits.
    /// </summary>
    [LuaIgnoreMember]
    public void FireProjectile(SkillProjectileRequest request)
    {
        var hero = Ecs.Get<PersonInfoComponent>(player).Hero;

        if (!Started || !actions.TryGetProjectile(request.Skill, hero, request.TriggerIndex, out var trigger))
        {
            return;
        }

        var flight = ProjectileFlight.Launch(trigger, request.Position, request.Right, request.Yaw, request.Direction, request.Target.Identifier);
        var session = ++ProjectileSession;

        if (!actions.IsFlownByClient(request.Skill))
        {
            player.Get<ServerSessionComponent>().Send(new SkillProjectileBroadcastResponse
            {
                Owner = Ecs.Get<ActorComponent>(player),
                Projectile = default(ProjectileShotValue) with
                {
                    Skill = request.Skill,
                    Attack = request.TriggerIndex,
                    Position = flight.Position,
                    Direction = flight.Direction,
                    Session = session
                }
            });
        }

        var position = default(SkillPositionValue) with { Position = request.Position, Angle = request.Yaw };

        _ = FlyAsync(request.Skill, trigger, flight, position);
    }

    private async Task FlyAsync(int skill, AttackTrigger trigger, ProjectileFlight flight, SkillPositionValue position)
    {
        try
        {
            var last = Stopwatch.GetTimestamp();

            while (!Closed && !flight.IsOver)
            {
                await Task.Delay(FlightTick);

                if (Closed)
                {
                    return;
                }

                var seconds = (float)Stopwatch.GetElapsedTime(last).TotalSeconds;

                last = Stopwatch.GetTimestamp();

                foreach (var explosion in flight.Advance(seconds, creatureManager.GetFlightTargets()))
                {
                    await ExplodeAsync(skill, trigger, explosion, position);

                    if (Closed)
                    {
                        return;
                    }
                }
            }
        }

        catch (Exception e)
        {
            Console.WriteLine($"[{nameof(FlyAsync)}]: {e}");
        }
    }

    private ValueTask ExplodeAsync(int skill, AttackTrigger trigger, Explosion explosion, SkillPositionValue position)
    {
        var yaw = Ecs.Get<WorldComponent>(player).Rotation;
        var area = new AttackArea(trigger, explosion.Position, yaw);
        var targets = trigger.Projectile is { TargetOnly: true }
            ? []
            : creatureManager.FindMonsters((place, body) => area.Touches(place, body.BodyRadius, body.BodyHeight));

        if (explosion.Collider is { } key && creatureManager.FindMonster(key) is { } collider && !targets.Contains(collider))
        {
            targets = [.. targets, collider];
        }

        return ApplyDamageAsync(skill, trigger, targets, position);
    }

    private async ValueTask ApplyDamageAsync(int skill, AttackTrigger trigger, Entity[] targets, SkillPositionValue position)
    {
        if (targets.Length == 0 || !trigger.AffectsTargets)
        {
            return;
        }

        var session = player.Get<ServerSessionComponent>();
        var attacker = Ecs.Get<WorldComponent>(player);

        var attackerStats = combat.GetPerson(Ecs.Get<PersonInfoComponent>(player).Hero, PersonModuleDefines.DefaultLevel);
        var skillDamage = combat.GetSkill(skill);
        var hits = targets.Select(target => ApplyDamage(target, attackerStats, skillDamage, trigger.DamageRate)).ToArray();

        session.Send(new SkillActionBroadcastResponse
        {
            SkillDmgValueList = hits,
            SkillAction = default(SkillActionValue) with
            {
                SkillActorInfo = default(SkillActorInfoValue) with
                {
                    UxActorId = Ecs.Get<ActorComponent>(player),
                    SkillPosInfo = default(SkillActionPosInfoValue) with
                    {
                        Position = attacker.Position,
                        Angle = attacker.Rotation,
                        MotionClass = position.MotionClass,
                        HasInputFlag = position.HasInputFlag
                    }
                },
                Skill = skill,
                TriggerIndex = (short)trigger.Id,
                ContinuousHit = 1
            }
        });

        // Another hit can land while a script runs, so the dead leave the fight before the first script starts.
        var dead = targets
            .Where(e => Ecs.Get<CreatureComponent>(e).Health == 0)
            .Select(target => RemoveDead(session, target))
            .ToArray();

        foreach (var (identifier, place) in dead)
        {
            if (Closed)
            {
                return;
            }

            await State.OnDie(identifier, place.SpawnBox, this);
        }

        foreach (var sector in dead.Select(e => e.Place.Sector).Distinct())
        {
            if (Closed)
            {
                return;
            }

            await CheckSectorAsync(sector);
        }
    }

    private SkillDmgValue ApplyDamage(Entity target, in CombatStats attacker, in SkillDamage skill, float attackRate)
    {
        var creature = Ecs.Get<CreatureComponent>(target);
        var body = Ecs.Get<MonsterCombatComponent>(target);
        var result = DamageCalculator.Calculate(attacker, body.Stats, skill, body.Defense, attackRate, Random.Shared);
        var lost = result.Damage;

        if (body.CountsHits)
        {
            var now = Environment.TickCount64;

            lost = result.Damage > 0 && now - body.LastCountedHit >= CountedHitMilliseconds ? 1 : 0;

            if (lost > 0)
            {
                Ecs.Set(target, body with { LastCountedHit = now });
            }
        }

        var health = Math.Max(0, creature.Health - lost);

        Ecs.Set(target, creature with { Health = health });

        return default(SkillDmgValue) with
        {
            UxActorId = Ecs.Get<ActorComponent>(target),
            DamageFlags = (byte)result.Flags,
            Damage = result.Damage,
            Health = health
        };
    }

    private async ValueTask CheckSectorAsync(int sector)
    {
        var kills = creatureManager.GetKillProgress(sector);

        if (kills.IsCleared(creatureManager.CountBoxesToCome(sector)))
        {
            await CompleteSectorAsync(sector);
        }

        if (!Closed)
        {
            await CheckSpawnStepAsync(sector);
        }
    }

    /// <summary>
    /// Enough dead open the next wave of a sector, unless the script holds it back.
    /// </summary>
    private async ValueTask CheckSpawnStepAsync(int sector)
    {
        var kills = creatureManager.GetKillProgress(sector);

        if (kills.Stopped)
        {
            return;
        }

        if (!kills.IsCurrentStepSpawned)
        {
            SpawnSector(sector);

            return;
        }

        if (!kills.TryOpenNextStep())
        {
            return;
        }

        await State.OnCompleteSpawnStepCondition(sector, kills.Step, this);

        if (!Closed && !kills.Stopped)
        {
            SpawnSector(sector);
        }
    }

    private void WakeSectorBehindGate(int gate, bool open)
    {
        if (open && Batch.EventBox.ServerGates.FirstOrDefault(e => e.Id == gate) is { NextSector: > 0 } box)
        {
            SpawnSector(box.NextSector);
        }
    }

    private (int Identifier, NonPlayableCreatureComponent Place) RemoveDead(ServerSessionComponent session, Entity target)
    {
        var identifier = Ecs.Get<CreatureComponent>(target).Identifier;
        var place = Ecs.Get<NonPlayableCreatureComponent>(target);

        session.Send(new MonsterDieResponse { Actor = Ecs.Get<ActorComponent>(target) });

        creatureManager.Remove(target);

        return (identifier, place);
    }

    /// <summary>
    /// Clearing a sector opens its gates, and what lies behind them comes to life.
    /// </summary>
    private async ValueTask CompleteSectorAsync(int sector)
    {
        var box = Batch.EventBox.Sectors.FirstOrDefault(e => e.Id == sector);

        // Other sectors are finished by their script.
        if (box is null || box.ClearType is not (ClearType.Kill or ClearType.KillPerpect) || Progress.IsSectorCleared(sector))
        {
            return;
        }

        SectorClear(sector);

        foreach (var gate in Batch.EventBox.ServerGates.Where(e => e.Sector == sector))
        {
            SetGateFlag(gate.Id, true);
        }

        await State.OnCompleteSector(sector, this);
    }

    private async Task RunLaterAsync(float seconds, Func<ValueTask> action)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds));

            if (!Closed)
            {
                await action();
            }
        }

        catch (Exception e)
        {
            Console.WriteLine($"[{nameof(RunLaterAsync)}]: {e}");
        }
    }

    /// <summary>
    /// True only the first time: the script must see a player enter once.
    /// </summary>
    public bool TryStart()
    {
        if (Started)
        {
            return false;
        }

        Started = true;

        return true;
    }
    
    [LuaMember]
    public void AcceptQuest(int episode)
    {
        questManager.AcceptEpisode(player, episode);
    }

    [LuaMember]
    public void AddEventMaxTime()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void AddEventTimer(string function, string step, string ready, int type, float time, int param1, int param2)
    {
        Console.WriteLine($"[{nameof(AddEventTimer)}]: {function}, {step}, {ready}, {type}, {time}, {param1}, {param2}");
    }

    [LuaMember]
    public void AddLuaValue(int index, int value)
    {
        Console.WriteLine($"[{nameof(AddLuaValue)}]: {index}, {value}");
    }

    [LuaMember]
    public void AddTimer(int identifier, float time)
    {
        Console.WriteLine($"[{nameof(AddTimer)} start] {identifier} {time}");
        
        _ = RunLaterAsync(time, () => State.OnTimerComplete(identifier, this));
    }

    [LuaMember]
    public void AddTimerEx(string function, float time, int param1, int param2, int param3)
    {
        Console.WriteLine($"[{nameof(AddTimerEx)} start] {function} {param1} {param2} {param3}");
        
        _ = RunLaterAsync(time, () => State.OnTimerExComplete(function, param1, param2, param3, this));
    }

    [LuaMember]
    public void AllDestroySectorMonster()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void AllUserWarpInSector()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void ApplyBuff(int actor, int buff)
    {
        Console.WriteLine($"[{nameof(ApplyBuff)}]: {actor}, {buff}");
        
        buffManager.Apply(player, actor, buff);
    }

    [LuaMember]
    public void ChangeMonster()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void ChangeNpcAnimation()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void ChatMessage()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void ClearAllTimers()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void CompleteCondition()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void CompleteQuest(int episode)
    {
        questManager.CompleteEpisode(player, episode);
    }

    [LuaMember]
    public void CompleteTimerStep(string function, int step)
    {
        Console.WriteLine($"[{nameof(CompleteTimerStep)}]: {function}, {step}");
    }

    [LuaMember]
    public void EnableInteractionBox(int box, bool enable)
    {
        if (Progress.SetInteraction(box, enable) is { } value)
        {
            player.Get<ServerSessionComponent>().Send(new MazeInteractionEnableResponse
            {
                Show = value.Show,
                Enable = value.Enable,
                BoxIndex = value.Box,
                CallCount = value.CallCount
            });
        }
    }

    [LuaMember("ExcutEventSpawnGroupLua")]
    public void ExecuteEventSpawnGroupLua(int group)
    {
        Console.WriteLine($"[{nameof(ExecuteEventSpawnGroupLua)}]: {group}");
    }

    [LuaMember("ExcutEventSpawnLua")]
    public void ExecuteEventSpawnLua(int box)
    {
        Console.WriteLine($"[{nameof(ExecuteEventSpawnLua)} start]: {box}");
        
        creatureManager.Create(player, box);
        
        Console.WriteLine($"[{nameof(ExecuteEventSpawnLua)} end]: {box}");
    }

    [LuaMember]
    public void ExecuteDestroy(int type, string table, string animation, bool suicide = true)
    {
        Console.WriteLine($"[{nameof(ExecuteDestroy)}]: {type}, {table}, {animation}, {suicide}");
    }
    
    [LuaMember]
    public VVector3 GetCommonPositionBox(int box)
    {
        Console.WriteLine($"[{nameof(GetCommonPositionBox)}]: {box}");

        return Batch.EventBox.CommonPositions
            .First(x => x.Id == box)
            .GetRandomPosition();
    }

    [LuaMember]
    public void GetLuaValue(int index)
    {
        Console.WriteLine($"[{nameof(GetLuaValue)}]: {index}");
    }

    [LuaMember]
    public void InitPartyQuest()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public bool IsHaveCondition(int conditionId)
    {
        return questManager.ActiveEpisodeUsesCondition(player, conditionId);
    }

    [LuaMember]
    public bool IsHaveQuest(int episodeId)
    {
        return questManager.HasActiveEpisode(player, episodeId);
    }

    [LuaMember]
    public void LuaClientSync(int type, int value, int sector = 0, float time = 0)
    {
        Console.WriteLine($"[{nameof(LuaClientSync)}]: {type}, {value}, {sector}, {time}");
    }

    [LuaMember]
    public void MoveNpcToWayPoint(int npc, int waypoint)
    {
        Console.WriteLine($"[{nameof(MoveNpcToWayPoint)}]: {npc}, {waypoint}");
        
        creatureManager.MoveToPoint(npc, waypoint);
    }

    [LuaMember]
    public void RemoveQuestAll()
    {
        questManager.RemoveAllActiveEpisodes(player);
    }

    [LuaMember]
    public void RemoveTimerEx()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void SectorClear(int sector)
    {
        if (Progress.ClearSector(sector))
        {
            player.Get<ServerSessionComponent>().Send(new MazeClearSectorResponse { Sector = sector, Status = true });
        }
    }

    [LuaMember]
    public void SetEscortCondition()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void SetEscortMonster()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void SetGateFlag(int gate, bool open)
    {
        if (Progress.SetGate(gate, open))
        {
            player.Get<ServerSessionComponent>().Send(new WorldGateEnableResponse { Gate = gate, Open = open });

            WakeSectorBehindGate(gate, open);
        }
    }

    [LuaMember]
    public void SetGateFlagNoSend(int gate, bool open)
    {
        if (Progress.SetGate(gate, open))
        {
            WakeSectorBehindGate(gate, open);
        }
    }

    [LuaMember]
    public void PrivateSetLuaValue(LuaValue keyOrIndex, LuaValue value)
    {
        if (keyOrIndex.TryRead<string>(out var key))
        {
            PrivateSetLuaValue(key);
            return;
        }
        
        PrivateSetLuaValue(keyOrIndex.Read<int>(), value.Read<int>());
    }
    
    [LuaIgnoreMember]
    private void PrivateSetLuaValue(string key)
    {
        Console.WriteLine($"[{nameof(PrivateSetLuaValue)}]: {key}");
    }
    
    [LuaIgnoreMember]
    private void PrivateSetLuaValue(int index, int value)
    {
        Console.WriteLine($"[{nameof(PrivateSetLuaValue)}]: {index}, {value}");
    }

    /// <summary>
    /// Lua-CSharp is not supports enum as parameter right now
    /// </summary>
    [LuaMember]
    public void SetMazeState(int state)
    {
        if (Enum.IsDefined(typeof(MazeState), state))
        {
            SetMazeState((MazeState)state);
        }
        else
        {
            Console.WriteLine($"[{nameof(SetMazeState)}]: Invalid maze state ID: {state}");
        }
    }

    [LuaIgnoreMember]
    public void SetMazeState(MazeState state)
    {
        Console.WriteLine($"[{nameof(SetMazeState)}]: {state}");
    }

    [LuaMember]
    public void SetMonsterAllowPassiveType()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void SetMonsterDefenceType()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void SetNpcRotation()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void SetPotalFlag(int box, bool flag)
    {
        if (Progress.SetPortal(box, flag))
        {
            player.Get<ServerSessionComponent>().Send(new WorldWarpEnableResponse { Box = box, Open = flag });

            if (flag && Batch.EventBox.Portals.FirstOrDefault(e => e.Id == box) is { NextSector: > 0 } portal)
            {
                SpawnSector(portal.NextSector);
            }
        }
    }

    [LuaMember]
    public void SetSectorStepStop(int sector, bool stop)
    {
        var kills = creatureManager.GetKillProgress(sector);

        if (kills.Stopped == stop)
        {
            return;
        }

        kills.Stopped = stop;

        if (!stop)
        {
            _ = RunLaterAsync(0f, () => CheckSpawnStepAsync(sector));
        }
    }

    [LuaMember]
    public void SetTrapLifeTime(int index, float time)
    {
        Console.WriteLine($"[{nameof(SetTrapLifeTime)}]: {index}, {time}");
    }

    [LuaMember]
    public void ShowCasualRaidTimer()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void StartDefenceMode()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void StartEventTimer(string function)
    {
        Console.WriteLine($"[{nameof(StartEventTimer)}]: {function}");
    }

    [LuaMember]
    public void StartOperationMode()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void StartSurvivalMode()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void TerminateSpawn()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void TerminateSpawnBox()
    {
        throw new NotImplementedException();
    }
}