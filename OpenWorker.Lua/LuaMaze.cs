using System.Diagnostics;
using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using Lua;
using Microsoft.Extensions.Logging;
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
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Hotspot;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Domain.Batch.Enums;
using OpenWorker.Hotspot.Modules.Maze.Responses;
using OpenWorker.Hotspot.Modules.Maze.Types;
using OpenWorker.Hotspot.Modules.Monster.Responses;
using OpenWorker.Hotspot.Modules.Skill.Requests;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Hotspot.Modules.Chat.Responses;
using OpenWorker.Hotspot.Modules.Persons.Responses;
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
    CombatTables combat,
    MazeInfoRow info,
    MazeReporter reports,
    ILogger<LuaMaze> logger
) : ILuaUserData
{
    public LuaState State { get; } = state;
    
    public World Ecs { get; } = ecs;
    public VBatchFile Batch { get; } = batch;

    /// <summary>
    /// Where the maze, its script events and its script calls write to.
    /// </summary>
    internal ILogger Logger { get; } = logger;

    public MazeProgress Progress { get; } = new(batch, interactions);

    /// <summary>
    /// The monsters of the maze and the players they fight.
    /// </summary>
    public LuaCreatureManager Creatures => creatureManager;

    /// <summary>
    /// Where a trap stands. It is a class because the player can move it while it hits.
    /// </summary>
    private sealed class TrapSite(Vector3 position)
    {
        public Vector3 Position { get; set; } = position;
    }

    /// <summary>
    /// A timer of the script that fails must not stop the others, so errors end here.
    /// </summary>
    private sealed class TimerHost(LuaMaze maze) : IMazeTimerHost
    {
        public async ValueTask CallAsync(string function, int first, int second, int third)
        {
            if (maze.Closed)
            {
                return;
            }

            try
            {
                await maze.State.OnTimerExComplete(function, first, second, third, maze);
            }

            catch (Exception e) when (e is not OperationCanceledException)
            {
                maze.Logger.TimerFunctionFailed(e, function);
            }
        }

        public async ValueTask CompleteAsync(int timer)
        {
            if (maze.Closed)
            {
                return;
            }

            try
            {
                await maze.State.OnTimerComplete(timer, maze);
            }

            catch (Exception e) when (e is not OperationCanceledException)
            {
                maze.Logger.TimerFailed(e, timer);
            }
        }

        public void ShowTimer(int value, float time, float next)
        {
            maze.ShowTimer(value, time, next);
        }
    }

    // The boxes of the cubes that are taken by holding the click have this in the name of their object.
    private const string CubeKey = "VaccumCube";

    // A monster that counts hits takes them no faster than this.
    private const long CountedHitMilliseconds = 500;

    private static readonly TimeSpan FlightTick = TimeSpan.FromMilliseconds(50);

    private static readonly TimeSpan TimerTick = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// Townsfolk do not move here, so a script that waits for one to arrive hears of it after this long.
    /// </summary>
    private const float NpcMoveSeconds = 1f;

    /// <summary>
    /// A group of mazes has four episodes.
    /// </summary>
    private const int MaxEpisode = 4;

    private const byte CutsceneStopped = 0;
    private const byte CutscenePlaying = 2;

    private int SkillGeneration { get; set; }
    private int CurrentSkill { get; set; }
    private TrapSite? MouseTrap { get; set; }
    private SkillPlayback? Playback { get; set; }
    private CancellationTokenSource? SkillWake { get; set; }
    private Queue<bool> PressedKeys { get; } = [];
    private uint ProjectileSession { get; set; }
    private bool Started { get; set; }
    private long StartedAt { get; set; }
    private bool Closed { get; set; }
    private bool TimersRunning { get; set; }
    private MazeTimers? _timers;

    /// <summary>
    /// What the script has stored with <c>SetLuaValue</c>: numbers that stay for as long as the maze does.
    /// </summary>
    private Dictionary<int, int> LuaValues { get; } = [];

    private List<string> LuaKeys { get; } = [];
    private Dictionary<int, bool> SwitchNpcs { get; } = [];

    /// <summary>
    /// The cutscene the player is watching, if any.
    /// </summary>
    private string? Cutscene { get; set; }

    /// <summary>
    /// The maze is won: its last sector has been cleared. Doors that open from now on say so to the client.
    /// </summary>
    public bool IsComplete { get; private set; }

    public MazeState MazeState { get; private set; } = MazeState.None;

    private MazeTimers Timers => _timers ??= new MazeTimers(new TimerHost(this));

    /// <summary>
    /// The player has left: timers must not touch an entity that is gone, or that belongs to someone else by now.
    /// </summary>
    public void Close()
    {
        Closed = true;

        Timers.Clear();
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

        CurrentSkill = skill;

        // A trap follows the cursor only while its skill is being played.
        MouseTrap = null;
        Playback = null;
        SkillWake?.Cancel();

        if (actions.GetAnimations(skill, hero) is not { } animations)
        {
            return;
        }

        var playback = new SkillPlayback(animations);

        Playback = playback;
        PressedKeys.Clear();

        _ = PlayAsync(skill, playback, position, generation, Stopwatch.GetTimestamp());
    }

    /// <summary>
    /// The player holds the attack key or lets it go. What it means depends on the skill that is being played.
    /// </summary>
    [LuaIgnoreMember]
    public void PressKey(bool pressed)
    {
        if (Playback is { IsDone: false })
        {
            PressedKeys.Enqueue(pressed);
            SkillWake?.Cancel();
        }
    }

    /// <summary>
    /// Follows a skill through its steps: each attack is judged at its moment, and the key can cut a step short.
    /// </summary>
    private async Task PlayAsync(int skill, SkillPlayback playback, SkillPositionValue position, int generation, long began)
    {
        try
        {
            while (!Closed && generation == SkillGeneration && playback.NextTime is { } next)
            {
                var wait = next - (float)Stopwatch.GetElapsedTime(began).TotalSeconds;

                if (wait > 0f && PressedKeys.Count == 0)
                {
                    using var wake = new CancellationTokenSource();

                    SkillWake = wake;

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(wait), wake.Token);
                    }

                    catch (OperationCanceledException)
                    {
                    }

                    finally
                    {
                        // The next skill may have taken its place already.
                        if (ReferenceEquals(SkillWake, wake))
                        {
                            SkillWake = null;
                        }
                    }

                    if (Closed || generation != SkillGeneration)
                    {
                        return;
                    }
                }

                var now = (float)Stopwatch.GetElapsedTime(began).TotalSeconds;

                foreach (var hit in playback.Advance(now))
                {
                    _ = RunLaterAsync(0f, () =>
                    {
                        if (generation != SkillGeneration)
                        {
                            return ValueTask.CompletedTask;
                        }

                        return hit.Trigger.IsTrap ? PlaceTrapAsync(skill, hit.Trigger, position) : ApplyHitAsync(skill, hit, position);
                    });
                }

                while (PressedKeys.TryDequeue(out var pressed))
                {
                    playback.Press(pressed, now);
                }
            }

            // The skill is over, and a trap that followed the cursor stays where it is.
            if (generation == SkillGeneration)
            {
                MouseTrap = null;
            }
        }

        catch (Exception e)
        {
            Logger.SkillPlaybackFailed(e);
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
        var (sin, cos) = MathF.SinCos(attacker.Rotation * MathF.PI / 180f);
        var site = new TrapSite(attacker.Position);

        if (actions.FollowsMouse(skill))
        {
            // One trap follows the cursor at a time, a second attack of the skill only keeps it company.
            if (MouseTrap is not null)
            {
                return;
            }

            MouseTrap = site;
        }

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
            // The place is read again at every hit: the player may have moved the trap since the last one.
            var area = new AttackArea(trigger, site.Position, attacker.Rotation);
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

    /// <summary>
    /// The player drags the trap of a skill with the mouse. Nobody else shares this maze to be told about it.
    /// </summary>
    [LuaIgnoreMember]
    public void MoveTrap(Vector3 place)
    {
        if (Started && MouseTrap is { } trap)
        {
            trap.Position = place;
        }
    }

    /// <summary>
    /// The client lets a chain go when its animation gets there. The chain jumps between monsters on the server,
    /// and the client draws it from what it is told: who it holds and when it lets go.
    /// </summary>
    [LuaIgnoreMember]
    public void FireChain(SkillProjectileValue request)
    {
        var hero = Ecs.Get<PersonInfoComponent>(player).Hero;

        // Only the skill that is being played may let a chain go.
        if (!Started || request.Skill != CurrentSkill || !actions.TryGetChain(request.Skill, hero, request.TriggerIndex, out var trigger))
        {
            return;
        }

        var attacker = Ecs.Get<WorldComponent>(player);
        var chain = ChainLightning.Fire(trigger, attacker.Position, attacker.Rotation, creatureManager.GetFlightTargets());
        var session = ++ProjectileSession;
        var first = chain.Targets.Count > 0 ? new ActorValue(chain.Targets[0], ActorType.Monster) : (ActorValue)(-1);

        player.Get<ServerSessionComponent>().Send(new SkillChainBroadcastResponse
        {
            Owner = Ecs.Get<ActorComponent>(player),
            SkillChainPosition = default(SkillChainPosition) with
            {
                Skill = request.Skill,
                TriggerIndex = request.TriggerIndex,
                Position = attacker.Position,
                Direction = new Vector3(0f, -1f, 0f) * actions.GetMaxRange(request.Skill),
                Session = session,
                Target = first
            }
        });

        var position = default(SkillPositionValue) with { Position = attacker.Position, Angle = attacker.Rotation };

        _ = ChainAsync(request.Skill, trigger, chain, session, position, SkillGeneration);
    }

    /// <summary>
    /// A chain belongs to the skill that let it go: it lives its time, or until the player starts another skill.
    /// </summary>
    private async Task ChainAsync(int skill, AttackTrigger trigger, ChainLightning chain, uint session, SkillPositionValue position, int generation)
    {
        try
        {
            var last = Stopwatch.GetTimestamp();
            var hits = new List<ChainHit>();
            var changes = new List<ChainChange>();

            while (!Closed && !chain.IsOver && generation == SkillGeneration)
            {
                await Task.Delay(FlightTick);

                if (Closed || generation != SkillGeneration)
                {
                    return;
                }

                var seconds = (float)Stopwatch.GetElapsedTime(last).TotalSeconds;
                var owner = Ecs.Get<WorldComponent>(player);

                last = Stopwatch.GetTimestamp();

                hits.Clear();
                changes.Clear();

                chain.Advance(seconds, creatureManager.GetFlightTargets(), owner.Position, owner.Rotation, hits, changes);

                foreach (var change in changes)
                {
                    player.Get<ServerSessionComponent>().Send(new SkillChainUpdateTargetResponse
                    {
                        Session = session,
                        IsAdd = change.Added,
                        Target = new ActorValue(change.Target, ActorType.Monster)
                    });
                }

                if (hits.Count > 0)
                {
                    await StrikeChainAsync(skill, trigger, hits, position);
                }
            }
        }

        catch (Exception e)
        {
            Logger.ChainFailed(e);
        }
    }

    /// <summary>
    /// Everyone the chain reaches at one moment is hit together. The links after the first carry a share of the damage.
    /// </summary>
    private ValueTask StrikeChainAsync(int skill, AttackTrigger trigger, List<ChainHit> hits, SkillPositionValue position)
    {
        var targets = new List<Entity>();
        var shares = new List<float>();

        foreach (var hit in hits)
        {
            if (creatureManager.FindMonster(hit.Target) is { } monster)
            {
                targets.Add(monster);
                shares.Add(hit.Order > 0 ? trigger.Chain!.DamageMultiple : 1f);
            }
        }

        return ApplyDamageAsync(skill, trigger, [.. targets], position, [.. shares]);
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
            Logger.ProjectileFlightFailed(e, skill);
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

    /// <param name="shares">The share of the damage each target takes, by their place in the list. Everyone takes all of it without.</param>
    private async ValueTask ApplyDamageAsync(int skill, AttackTrigger trigger, Entity[] targets, SkillPositionValue position, float[]? shares = null)
    {
        if (targets.Length == 0 || !trigger.AffectsTargets)
        {
            return;
        }

        var session = player.Get<ServerSessionComponent>();
        var attacker = Ecs.Get<WorldComponent>(player);

        var attackerStats = combat.GetPerson(Ecs.Get<PersonInfoComponent>(player).Hero, PersonModuleDefines.DefaultLevel, Ecs.Get<TitleComponent>(player));
        var skillDamage = combat.GetSkill(skill);
        var hits = targets.Select((target, i) => ApplyDamage(target, attackerStats, skillDamage, trigger.DamageRate * (shares?[i] ?? 1f), trigger.ReactionType)).ToArray();

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
            .Select((target, i) => (Target: target, Hit: hits[i]))
            .Where(e => Ecs.Get<CreatureComponent>(e.Target).Health == 0)
            .Select(e => (Removed: RemoveDead(session, e.Target), e.Hit))
            .ToArray();

        var kind = actions.GetPassiveType(skill);

        foreach (var ((identifier, place), hit) in dead)
        {
            if (Closed)
            {
                return;
            }

            await reports.MonsterKilledAsync(player, identifier, MazeReporter.GetKillConditions(kind, (hit.DamageFlags & (byte)HitResults.Critical) != 0));

            await State.OnDie(identifier, place.SpawnBox, this);
        }

        foreach (var sector in dead.Select(e => e.Removed.Place.Sector).Distinct())
        {
            if (Closed)
            {
                return;
            }

            await CheckSectorAsync(sector);
        }
    }

    private SkillDmgValue ApplyDamage(Entity target, in CombatStats attacker, in SkillDamage skill, float attackRate, int reaction)
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

        creatureManager.OnMonsterHit(target, player, result.Damage);

        var effect = creatureManager.AbsorbHit(target, skill.SuperArmorAttack, reaction, health > 0);

        return default(SkillDmgValue) with
        {
            UxActorId = Ecs.Get<ActorComponent>(target),
            ReactionType = effect.Reaction,
            DamageFlags = (byte)result.Flags,
            Damage = result.Damage,
            Health = health,
            DefenseType = effect.Defense,
            CurSuperArmorGauge = effect.SuperArmor
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
        creatureManager.SetGate(gate, open);

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
    /// A cleared sector tells its script, then the client, and opens its way out. What lies behind comes to life.
    /// </summary>
    private async ValueTask CompleteSectorAsync(int sector)
    {
        var box = Batch.EventBox.Sectors.FirstOrDefault(e => e.Id == sector);

        // Sectors of other kinds are finished by their script, or by the quests of the player.
        if (box is null || !Progress.ClearSector(sector))
        {
            return;
        }

        await FinishSectorAsync(box);
    }

    private async ValueTask FinishSectorAsync(SectorBox box)
    {
        await State.OnCompleteSector(box.Id, this);

        if (Closed)
        {
            return;
        }

        player.Get<ServerSessionComponent>().Send(new MazeClearSectorResponse { Sector = box.Id, Status = true });

        await reports.SectorClearedAsync(player, info.Id, box.Id);

        if (Closed)
        {
            return;
        }

        // The way out of the last sector leads out of the maze: the maze is won before the door says so.
        if (IsLastSector(box))
        {
            await CompleteMazeAsync(box);
        }

        OpenExit(box);
    }

    /// <summary>
    /// The client learns which maze of its group is cleared, and that the next episode is open. The mazes cleared
    /// before are not kept anywhere yet, so only this one is told.
    /// </summary>
    private void SendClearInfo()
    {
        var group = new MazeClearValue(info.Group, (byte)Math.Min(info.Episode + 1, MaxEpisode), [(info.Id, 1)]);

        player.Get<ServerSessionComponent>().Send(new MazeClearInfoResponse { Groups = [group], Person = Ecs.Get<ActorComponent>(player), IsReset = false });
    }

    /// <summary>
    /// The way out of a sector is a door or a portal, and it opens once the sector is cleared.
    /// </summary>
    private void OpenExit(SectorBox box)
    {
        if (box.Exit == 0)
        {
            return;
        }

        if (box.ExitType == SectorExitType.Gate)
        {
            SetGateFlag(box.Exit, true);
        }
        else
        {
            SetPotalFlag(box.Exit, true);
        }
    }

    /// <summary>
    /// The last sector is the one with the boss, or the one whose portal leads out of the maze. The maze is won
    /// when it is cleared, even if a calm sector with the way out lies behind it.
    /// </summary>
    private bool IsLastSector(SectorBox box)
    {
        if (box.Type == SectorType.Boss)
        {
            return true;
        }

        return box.ExitType == SectorExitType.Portal
            && Batch.EventBox.Portals.FirstOrDefault(e => e.Id == box.Exit) is { JumpType: not JumpType.JumpPos };
    }

    /// <summary>
    /// What happens once, when the last sector is cleared: the rest of its monsters go, the clock stops and the
    /// client is told that the maze is won.
    /// </summary>
    private async ValueTask CompleteMazeAsync(SectorBox last)
    {
        if (IsComplete)
        {
            return;
        }

        IsComplete = true;
        MazeState = MazeState.None;

        SendClearInfo();

        await reports.MazeClearedAsync(player, info, (int)Stopwatch.GetElapsedTime(StartedAt).TotalSeconds, (int)Ecs.Get<PersonInfoComponent>(player).Hero);

        await DestroyAsync(creatureManager.FindMonsters((_, _) => true).Where(e => Ecs.Get<NonPlayableCreatureComponent>(e).Sector == last.Id).ToArray(), false);
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
            Logger.DelayedActionFailed(e);
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
        StartedAt = Stopwatch.GetTimestamp();

        return true;
    }

    /// <summary>
    /// The objects of soul metries stand in the maze all the time, but only the ones the person is to find this
    /// time are shown. This is called before the client is told what the maze holds.
    /// </summary>
    public void HideUnfoundSoulObjects()
    {
        var objects = reports.GetSoulObjects(info.Id);

        if (objects.Count == 0)
        {
            return;
        }

        var shown = reports.GetSoulObjectsToShow(player, info.Id);

        foreach (var box in Batch.EventBox.InterActions.Where(e => objects.Contains(e.Interaction) && !shown.Contains(e.Interaction)))
        {
            Progress.HideInteraction(box.Id);
        }
    }

    /// <summary>
    /// The person clicked an object of the maze: it counts for the quests, and if it is one of a soul metry, the soul
    /// metry finds it.
    /// </summary>
    public async ValueTask CollectSoulObjectAsync(int box)
    {
        await reports.ObjectUsedAsync(player, box);

        if (Closed)
        {
            return;
        }

        if (Batch.EventBox.InterActions.FirstOrDefault(e => e.Id == box) is { } found)
        {
            await reports.CollectSoulObjectAsync(player, info.Id, found.Interaction);
        }
    }

    /// <summary>
    /// The cubes of the maze, which the person takes by holding the click for the time the table gives the object.
    /// </summary>
    public void RegisterCubes()
    {
        foreach (var box in Batch.EventBox.InterActions.Where(e => e.ObjectKey.Contains(CubeKey, StringComparison.OrdinalIgnoreCase)))
        {
            var row = interactions.FirstOrDefault(e => e.Id == box.Interaction);

            reports.RegisterCube(player, box.Id, box.Interaction, TimeSpan.FromMilliseconds(row.ActionDelay));
        }
    }
    
    [LuaMember]
    public void AcceptQuest(int episode)
    {
        questManager.AcceptEpisode(player, episode);
    }

    /// <summary>
    /// Gives a timer with steps more time.
    /// </summary>
    [LuaMember]
    public void AddEventMaxTime(string function, float time)
    {
        Timers.Extend(function, time);
    }

    /// <param name="function">Called when the time of a step is up. It also names the timer.</param>
    /// <param name="step">Called when the script finishes a step early.</param>
    /// <param name="ready">Called when the waiting before a step begins.</param>
    /// <param name="type">Only timers with steps are known.</param>
    /// <param name="time">How long a step takes.</param>
    /// <param name="steps">How many steps there are.</param>
    /// <param name="wait">How long it waits before each step.</param>
    [LuaMember]
    public void AddEventTimer(string function, string step, string ready, int type, float time, int steps, int wait)
    {
        if (type != MazeTimer.Stepped)
        {
            Logger.UnknownEventTimerType(type, function);
            return;
        }

        Timers.AddStepped(function, step, ready, time, steps, wait);
        RunTimers();
    }

    [LuaMember]
    public void AddLuaValue(int index, int value)
    {
        LuaValues[index] = LuaValues.GetValueOrDefault(index) + value;
    }

    [LuaMember]
    public void AddTimer(int identifier, float time)
    {
        Logger.CallAddTimer(identifier, time);

        Timers.AddPlain(identifier, time);
        RunTimers();
    }

    [LuaMember]
    public void AddTimerEx(string function, float time, int param1, int param2, int param3)
    {
        Logger.CallAddTimerEx(function, time, param1, param2, param3);

        Timers.AddPlain(function, time, param1, param2, param3);
        RunTimers();
    }

    /// <summary>
    /// Everything that lives in the sector dies at once and without a reward.
    /// </summary>
    [LuaMember]
    public void AllDestroySectorMonster(int sector)
    {
        _ = RunLaterAsync(0f, () => DestroyAsync(MonstersOf(sector), false));
    }

    [LuaMember]
    public void AllUserWarpInSector(VVector3 position, float rotation)
    {
        var place = new Vector3(position.X, position.Y, position.Z);
        var world = Ecs.Get<WorldComponent>(player);

        Ecs.Set(player, world with { Position = place, Rotation = rotation });

        player.Get<ServerSessionComponent>().Send(new MazeWarpInSectorResponse { Position = place, Rotation = rotation });
    }

    [LuaMember]
    public void ApplyBuff(int actor, int buff)
    {
        Logger.CallApplyBuff(actor, buff);
        
        if (!buffManager.Apply(player, actor, buff))
        {
            Logger.UnknownBuff(actor, buff);
        }
    }

    /// <summary>
    /// Monsters do not turn into others here: the script goes on with the old ones.
    /// </summary>
    [LuaMember]
    public void ChangeMonster(string from, string to)
    {
        Logger.IgnoredChangeMonster(from, to);
    }

    [LuaMember]
    public void ChangeNpcAnimation(int npc, string animation)
    {
        Logger.IgnoredChangeNpcAnimation(npc, animation);
    }

    /// <summary>
    /// A message to one player, or to everyone when the number is zero.
    /// </summary>
    [LuaMember]
    public void ChatMessage(int user, string message)
    {
        if (user == 0 || user == (int)Ecs.Get<ActorComponent>(player))
        {
            player.Get<ServerSessionComponent>().Send(new ChatNoticeResponse { Type = 0, Message = message, Color = "" });
        }
    }

    [LuaMember]
    public void ClearAllTimers()
    {
        Timers.Clear();
    }

    [LuaMember]
    public void CompleteCondition(int condition)
    {
        questManager.TryApplyConditionProgress(player, condition, 1);
    }

    [LuaMember]
    public void CompleteQuest(int episode)
    {
        questManager.CompleteEpisode(player, episode);
    }

    /// <summary>
    /// The script says a step of a timer is done before its time is up.
    /// </summary>
    [LuaMember]
    public void CompleteTimerStep(string function, int step)
    {
        Timers.Complete(function, step);
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
        Logger.IgnoredExecuteEventSpawnGroup(group);
    }

    [LuaMember("ExcutEventSpawnLua")]
    public void ExecuteEventSpawnLua(int box)
    {
        Logger.CallExecuteEventSpawn(box);

        creatureManager.Create(player, box);
    }

    /// <summary>
    /// Makes the monsters of a table die, wherever they are. Monsters are the type 2, townsfolk the type 1.
    /// </summary>
    [LuaMember]
    public void ExecuteDestroy(int type, string table, string animation, bool suicide = true)
    {
        if (type != 2 || !int.TryParse(table, out var identifier))
        {
            Logger.IgnoredExecuteDestroy(type, table, animation, suicide);
            return;
        }

        _ = RunLaterAsync(0f, () => DestroyAsync(
            creatureManager.FindMonsters((_, _) => true).Where(e => Ecs.Get<CreatureComponent>(e).Identifier == identifier).ToArray(),
            false));
    }

    [LuaMember]
    public VVector3 GetCommonPositionBox(int box)
    {
        Logger.CallGetCommonPositionBox(box);

        // A script that names a box the batch does not have keeps running, with the origin.
        if (Batch.EventBox.CommonPositions.FirstOrDefault(x => x.Id == box) is not { } position)
        {
            Logger.UnknownPositionBox(box);

            return new VVector3();
        }

        return position.GetRandomPosition();
    }

    [LuaMember]
    public int GetLuaValue(int index)
    {
        return LuaValues.GetValueOrDefault(index);
    }

    /// <summary>
    /// A quest of the party. There is no party in a maze of one.
    /// </summary>
    [LuaMember]
    public void InitPartyQuest(int quest, int condition, int sector)
    {
        Logger.IgnoredInitPartyQuest(quest, condition, sector);
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

    /// <summary>
    /// The client runs a function of its own script when it hears this.
    /// </summary>
    [LuaMember]
    public void LuaClientSync(int type, int value, int sector = 0, float time = 0)
    {
        player.Get<ServerSessionComponent>().Send(new MazeLuaClientSyncResponse { Type = type, Value = value });
    }

    [LuaMember]
    public void MoveNpcToWayPoint(int npc, int waypoint)
    {
        Logger.CallMoveNpcToWayPoint(npc, waypoint);

        creatureManager.MoveToPoint(npc, waypoint);
    }

    [LuaMember]
    public void RemoveQuestAll()
    {
        questManager.RemoveAllActiveEpisodes(player);
    }

    /// <summary>
    /// Stops a timer without telling the script, and says how long it had left.
    /// </summary>
    [LuaMember]
    public float RemoveTimerEx(string function)
    {
        return Timers.Cancel(function);
    }

    /// <summary>
    /// A sector the script clears is finished like one that was cleared by the fight.
    /// </summary>
    [LuaMember]
    public void SectorClear(int sector)
    {
        _ = RunLaterAsync(0f, () => CompleteSectorAsync(sector));
    }

    [LuaMember]
    public void SetEscortCondition(int condition)
    {
        Logger.IgnoredSetEscortCondition(condition);
    }

    [LuaMember]
    public void SetEscortMonster(LuaValue monster, LuaValue condition, LuaValue name)
    {
        Logger.IgnoredSetEscortMonster();
    }

    [LuaMember]
    public void SetGateFlag(int gate, bool open)
    {
        if (Progress.SetGate(gate, open))
        {
            var next = Batch.EventBox.ServerGates.FirstOrDefault(e => e.Id == gate)?.NextSector ?? 0;

            player.Get<ServerSessionComponent>().Send(new WorldGateEnableResponse { Gate = gate, Open = open, IsMazeComplete = IsComplete, NextSector = next });

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

    /// <summary>
    /// A key stays in the maze. A number goes under an index: <c>SetLuaValue(index, value)</c>.
    /// </summary>
    [LuaMember("SetLuaValue")]
    public void SetLuaValue(LuaValue keyOrIndex, LuaValue value)
    {
        if (keyOrIndex.TryRead<string>(out var key))
        {
            LuaKeys.Add(key);
            return;
        }

        LuaValues[keyOrIndex.Read<int>()] = value.Read<int>();
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
            Logger.UnknownMazeState(state);
        }
    }

    /// <summary>
    /// The state only tells scripts what the maze is doing. The client does not read it, and a maze waiting
    /// to die goes on by itself.
    /// </summary>
    [LuaIgnoreMember]
    public void SetMazeState(MazeState state)
    {
        Logger.CallSetMazeState(state);

        MazeState = state;

        if (state == MazeState.WaitToDie)
        {
            _ = RunLaterAsync(0f, () =>
            {
                if (MazeState == MazeState.WaitToDie)
                {
                    MazeState = MazeState.Run;
                }

                return ValueTask.CompletedTask;
            });
        }
    }

    [LuaMember]
    public void SetMonsterAllowPassiveType(int monster, int type)
    {
        Logger.IgnoredSetMonsterAllowPassiveType(monster, type);
    }

    [LuaMember]
    public void SetMonsterDefenceType(int monster, int type, float time)
    {
        Logger.IgnoredSetMonsterDefenceType(monster, type, time);
    }

    /// <summary>
    /// The townsman does not turn here, but the script gets word that it did.
    /// </summary>
    [LuaMember]
    public void SetNpcRotation(int npc, float angle, int key)
    {
        _ = RunLaterAsync(NpcMoveSeconds, () => State.OnNpcRotation(npc, key, this));
    }

    [LuaMember]
    public void SetPotalFlag(int box, bool flag)
    {
        if (Progress.SetPortal(box, flag))
        {
            player.Get<ServerSessionComponent>().Send(new WorldWarpEnableResponse { Box = box, Open = flag, IsMazeComplete = IsComplete });

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
        Logger.IgnoredSetTrapLifeTime(index, time);
    }

    /// <summary>
    /// A countdown on the screen: nothing counts without the client's own clock, so the number is the time to show.
    /// A negative value hides it.
    /// </summary>
    [LuaMember]
    public void ShowCasualRaidTimer(int value, float time, float next = 0f)
    {
        ShowTimer(value, time, next);
    }

    /// <summary>
    /// The game modes of waves and survival have tables of their own that are not read here.
    /// </summary>
    [LuaMember]
    public void StartDefenceMode(int table)
    {
        Logger.IgnoredStartMode(nameof(StartDefenceMode), table);
    }

    [LuaMember]
    public void StartEventTimer(string function)
    {
        Timers.Start(function);
    }

    [LuaMember]
    public void StartOperationMode(int table)
    {
        Logger.IgnoredStartMode(nameof(StartOperationMode), table);
    }

    [LuaMember]
    public void StartSurvivalMode(int table)
    {
        Logger.IgnoredStartMode(nameof(StartSurvivalMode), table);
    }

    /// <summary>
    /// No more monsters come to a sector. The sector itself is the current one when the number is zero.
    /// </summary>
    [LuaMember]
    public void TerminateSpawn(int sector)
    {
        if (sector == 0)
        {
            sector = CurrentSector;
        }

        creatureManager.GetKillProgress(sector).Stopped = true;

        foreach (var box in Batch.EventBox.MonsterSpawns.Where(e => e.Sector == sector))
        {
            creatureManager.Terminate(box.Id);
        }

        _ = RunLaterAsync(0f, () => CheckSectorAsync(sector));
    }

    /// <summary>
    /// A box that has not appeared yet never will. What has appeared stays.
    /// </summary>
    [LuaMember]
    public void TerminateSpawnBox(int box)
    {
        creatureManager.Terminate(box);

        if (Batch.EventBox.MonsterSpawns.FirstOrDefault(e => e.Id == box) is { } spawn)
        {
            _ = RunLaterAsync(0f, () => CheckSectorAsync(spawn.Sector));
        }
    }

    /// <summary>
    /// The sector the player stands in.
    /// </summary>
    private int CurrentSector => Batch.EventBox.Sectors.FirstOrDefault(e => Contains(e, Ecs.Get<WorldComponent>(player).Position))?.Id ?? 0;

    private static bool Contains(SectorBox box, Vector3 position)
    {
        return position.X >= Math.Min(box.PosTopLeft.X, box.PosBottomRight.X) &&
               position.X <= Math.Max(box.PosTopLeft.X, box.PosBottomRight.X) &&
               position.Y >= Math.Min(box.PosTopLeft.Y, box.PosBottomRight.Y) &&
               position.Y <= Math.Max(box.PosTopLeft.Y, box.PosBottomRight.Y);
    }

    private Entity[] MonstersOf(int sector)
    {
        return creatureManager.FindMonsters((_, _) => true).Where(e => Ecs.Get<NonPlayableCreatureComponent>(e).Sector == sector).ToArray();
    }

    /// <summary>
    /// Kills what is given: the client sees them die, and the sector counts them.
    /// </summary>
    /// <param name="scripts">Whether the script hears of each death.</param>
    private async ValueTask DestroyAsync(Entity[] targets, bool scripts)
    {
        var session = player.Get<ServerSessionComponent>();
        var dead = targets.Where(Ecs.IsAlive).Select(target => RemoveDead(session, target)).ToArray();

        foreach (var (identifier, place) in dead)
        {
            if (Closed)
            {
                return;
            }

            if (scripts)
            {
                await State.OnDie(identifier, place.SpawnBox, this);
            }
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

    /// <summary>
    /// Kills what a sector holds and finishes it, for a command that wants a sector done at once.
    /// </summary>
    [LuaIgnoreMember]
    public async ValueTask ClearSectorAsync(int sector)
    {
        if (sector == 0)
        {
            sector = CurrentSector;
        }

        await DestroyAsync(MonstersOf(sector), true);

        if (!Closed)
        {
            await CompleteSectorAsync(sector);
        }
    }

    /// <summary>
    /// A cutscene starts or is skipped on the client. The maze waits for the boss while one plays, and its script
    /// hears when it is over.
    /// </summary>
    [LuaIgnoreMember]
    public async ValueTask UpdateCutsceneAsync(bool play, string name)
    {
        if (!Started)
        {
            return;
        }

        var session = player.Get<ServerSessionComponent>();

        if (play)
        {
            Cutscene = name;

            SetMazeState(MazeState.StartBoss);

            session.Send(new PersonPlayCutsceneResponse { State = CutscenePlaying });

            return;
        }

        session.Send(new PersonPlayCutsceneResponse { State = CutsceneStopped });

        if (Cutscene is null)
        {
            return;
        }

        Cutscene = null;

        SetMazeState(MazeState.WaitToDie);

        await State.OnCutsceneOff(name, this);
    }

    /// <summary>
    /// A townsman that works as a switch stands one way or the other, and a click turns it over.
    /// </summary>
    [LuaIgnoreMember]
    public void ClickSwitchNpc(int npc)
    {
        var status = !SwitchNpcs.GetValueOrDefault(npc);

        SwitchNpcs[npc] = status;

        player.Get<ServerSessionComponent>().Send(new MazeSwitchNpcStatusResponse { Npc = npc, Status = status ? (byte)1 : (byte)0 });
    }

    private void ShowTimer(int value, float time, float next)
    {
        player.Get<ServerSessionComponent>().Send(new MazeTimerResponse { Value = value, Time = time, Next = next });
    }

    /// <summary>
    /// Timers count on the thread of the world, one tick at a time, while there are any.
    /// </summary>
    private void RunTimers()
    {
        if (TimersRunning || Closed)
        {
            return;
        }

        TimersRunning = true;

        _ = RunTimersAsync();
    }

    private async Task RunTimersAsync()
    {
        try
        {
            var last = Stopwatch.GetTimestamp();

            while (!Closed && !Timers.IsEmpty)
            {
                await Task.Delay(TimerTick);

                var seconds = (float)Stopwatch.GetElapsedTime(last).TotalSeconds;

                last = Stopwatch.GetTimestamp();

                if (!Closed)
                {
                    await Timers.UpdateAsync(seconds);
                }
            }
        }

        catch (Exception e)
        {
            Logger.TimersFailed(e);
        }

        finally
        {
            TimersRunning = false;
        }
    }
}
