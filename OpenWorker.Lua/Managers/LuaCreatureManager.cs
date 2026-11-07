using System.Diagnostics;
using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using OpenWorker.Batch;
using OpenWorker.Batch.Actions;
using OpenWorker.Batch.Entities;
using OpenWorker.Batch.Extensions;
using OpenWorker.Domain.Batch.Enums;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Ai;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Mapping;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Monster.Responses;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Hotspot.Modules.World.Types;
using OpenWorker.Lua.Ai;

namespace OpenWorker.Lua.Managers;

public sealed class LuaCreatureManager
{
    private static ComponentType[] Archetype { get; } = [
        ComponentRegistry.Add<ActorComponent>(),
        ComponentRegistry.Add<CreatureComponent>(),
        ComponentRegistry.Add<NonPlayableCreatureComponent>(),
        ComponentRegistry.Add<WorldComponent>(),
        ComponentRegistry.Add<MonsterCombatComponent>(),
    ];

    private const int NpcHealth = 250;

    /// <summary>
    /// How often the monsters of a maze think and move, in seconds.
    /// </summary>
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(50);

    private readonly World ecs;
    private readonly VBatchFile batch;
    private readonly CombatTables tables;

    public LuaCreatureManager(World ecs, VBatchFile batch, CombatTables tables, CreatureServices? services = null, ILogger? logger = null)
    {
        this.ecs = ecs;
        this.batch = batch;
        this.tables = tables;

        Services = services;
        Navigation = new SectorNavigation(batch.EventBox.Sectors);
        Vitals = new PlayerVitals(ecs, tables);
        Logger = logger;
    }

    internal CreatureServices? Services { get; }

    internal ILogger? Logger { get; }

    /// <summary>
    /// How much life the players of the maze have and who can be hurt.
    /// </summary>
    public PlayerVitals Vitals { get; }

    /// <summary>
    /// Where a monster can walk.
    /// </summary>
    public INavigation Navigation { get; set; }

    internal Random Random { get; } = new();

    /// <summary>
    /// Seconds since the maze was made.
    /// </summary>
    internal double Now => Stopwatch.GetElapsedTime(_started).TotalSeconds;

    private readonly long _started = Stopwatch.GetTimestamp();
    private int Offset { get; set; } = 1;
    private HashSet<int> SpawnedBoxes { get; } = [];
    private HashSet<Entity> Scenery { get; } = [];
    private bool Closed { get; set; }
    private bool Ticking { get; set; }

    private List<Entity> Creatures { get; } = [];
    private List<Entity> Players { get; } = [];
    private Dictionary<Entity, MonsterBody> Bodies { get; } = [];
    private Dictionary<Entity, DefenseType> Defenses { get; } = [];
    private Dictionary<int, SectorKillProgress> KillProgress { get; } = [];

    /// <summary>
    /// A player who is in the maze: the monsters see them, fight them, and the clients of all of them are told what happens.
    /// </summary>
    public void Attach(Entity player)
    {
        if (!Players.Contains(player))
        {
            Players.Add(player);
        }
    }

    public void Create(Entity player, int box)
    {
        Attach(player);

        // Maps can point at boxes they no longer have, and a client walks through such a trigger like any other.
        if (batch.EventBox.MonsterSpawns.FirstOrDefault(x => x.Id == box) is not { } spawn)
        {
            return;
        }

        SpawnedBoxes.Add(box);

        Console.WriteLine($"[{nameof(Create)}]: {spawn.LayerBitmask}");

        var created = new List<Entity>();

        foreach (var monster in spawn.MonsterList.Where(x => x.Id is not 0))
        {
            switch (monster.Type)
            {
                case MonsterSpawnType.Npc:
                    created.Add(CreateNpc(monster.Id, spawn));
                    break;

                case MonsterSpawnType.Monster:
                    created.Add(CreateMonster(monster.Id, spawn));
                    break;

                case MonsterSpawnType.DestructionObject or MonsterSpawnType.TreasureBox or MonsterSpawnType.Unit:
                    var scenery = CreateMonster(monster.Id, spawn);
                    Scenery.Add(scenery);
                    created.Add(scenery);
                    break;

                default:
                    // One kind of object the server cannot show must not stop the rest of the map.
                    Console.WriteLine($"[{nameof(Create)}]: skipped {monster.Id}, type {monster.Type}");
                    break;
            }
        }

        var session = ecs.Get<ServerSessionComponent>(player);

        switch (spawn.CreationCondition)
        {
            case CreationConditionType.Immediate:
                Show(session, created);
                break;

            case CreationConditionType.WaitSignal:
                _ = ShowLaterAsync(session, created, TimeSpan.FromSeconds(spawn.WaitCreationDelayTime));
                break;
        }
    }

    private void Show(ServerSessionComponent session, List<Entity> created)
    {
        var npcs = created.Where(e => ecs.Get<ActorComponent>(e).Type == ActorType.Npc).ToArray();
        var monsters = created.Except(npcs).ToArray();

        if (monsters.Length > 0)
        {
            session.Send(new WorldOtherInfosMonsterResponse
            {
                Monsters = monsters.Select(e => MonsterSnapshotMapper.ToStMonsterInfo(ecs, e, Bodies.TryGetValue(e, out var body) ? body.IsBattlePose : true)).ToArray()
            });
        }

        if (npcs.Length > 0)
        {
            session.Send(new WorldOtherInfosNpcResponse { List = npcs.Select(ToNpcEntry).ToArray() });
        }

        // From now on the clients know the monsters, so the monsters start to think.
        foreach (var monster in monsters)
        {
            if (Bodies.TryGetValue(monster, out var body))
            {
                body.Reveal();
            }
        }
    }

    private WorldNpcListEntry ToNpcEntry(Entity npc)
    {
        var creature = ecs.Get<CreatureComponent>(npc);
        var place = ecs.Get<WorldComponent>(npc);
        var nonPlayable = ecs.Get<NonPlayableCreatureComponent>(npc);

        return new WorldNpcListEntry(
            ecs.Get<ActorComponent>(npc),
            place.Position,
            place.Rotation,
            creature.Health,
            nonPlayable.Waypoint,
            nonPlayable.Sector,
            creature.Level,
            creature.Identifier);
    }

    private void Create(Entity creature, int identifier, MonsterSpawnBox spawn)
    {
        ecs.Set(creature, new CreatureComponent { Identifier = identifier, Health = NpcHealth });
        ecs.Set(creature, new NonPlayableCreatureComponent { SpawnBox = spawn.Id, Sector = spawn.Sector, Waypoint = spawn.Waypoint });
        ecs.Set(creature, new WorldComponent { Location = 0, Position = spawn.GetPosition(), Rotation = spawn.Rotation, Jump = 0 });
    }

    public Entity CreateMonster(int identifier, MonsterSpawnBox spawn)
    {
        var creature = ecs.Create(Archetype);

        ecs.Set(creature, new ActorComponent { Identifier = Offset++, Type = ActorType.Monster });

        Create(creature, identifier, spawn);

        var combat = tables.GetMonster(identifier);

        if (combat.Defense == DefenseType.SuperArmor)
        {
            var gauge = combat.Stats.MaxHealth * SuperArmorShare;

            combat = combat with { SuperArmor = gauge, MaxSuperArmor = gauge };
        }

        ecs.Set(creature, combat);
        ecs.Set(creature, ecs.Get<CreatureComponent>(creature) with { Level = combat.Stats.Level, Health = (int)combat.Stats.MaxHealth });

        if (combat.HoldsSector)
        {
            GetKillProgress(spawn.Sector).Added();
        }

        Creatures.Add(creature);

        GiveMind(creature, identifier, spawn.GetPosition());

        return creature;
    }

    /// <summary>
    /// A monster with a script thinks. Objects and the rest just stand.
    /// </summary>
    private void GiveMind(Entity creature, int identifier, Vector3 home)
    {
        if (Services is not { } services || services.Behaviors.Get(identifier) is not { } behavior)
        {
            return;
        }

        var profile = services.Scripts.Get(behavior.AiScript);

        // A script that could not be read leaves the monster without a mind.
        if (ReferenceEquals(profile, AiProfile.Empty))
        {
            return;
        }

        Bodies.Add(creature, new MonsterBody(ecs, this, services.Actions, creature, behavior, profile, home));

        StartTicking();
    }

    public Entity CreateNpc(int identifier, MonsterSpawnBox spawn)
    {
        var creature = ecs.Create(Archetype);

        ecs.Set(creature, new ActorComponent { Identifier = Offset++, Type = ActorType.Npc });

        Create(creature, identifier, spawn);

        Creatures.Add(creature);

        return creature;
    }

    /// <summary>
    /// Spawns the box the first time a sector asks for it: entering a sector again must not repeat it.
    /// </summary>
    public void SpawnOnce(Entity player, int box)
    {
        if (SpawnedBoxes.Add(box))
        {
            Create(player, box);
        }
    }

    public Entity[] FindMonsters(Func<Vector3, MonsterCombatComponent, bool> touches)
    {
        return Creatures
            .Where(e => ecs.Get<ActorComponent>(e).Type == ActorType.Monster)
            .Where(e => touches(ecs.Get<WorldComponent>(e).Position, ecs.Get<MonsterCombatComponent>(e)))
            .ToArray();
    }

    public Entity? FindMonster(int identifier)
    {
        foreach (var creature in Creatures)
        {
            var actor = ecs.Get<ActorComponent>(creature);

            if (actor.Type == ActorType.Monster && actor.Identifier == identifier)
            {
                return creature;
            }
        }

        return null;
    }

    public FlightTarget[] GetFlightTargets()
    {
        return Creatures
            .Where(e => ecs.Get<ActorComponent>(e).Type == ActorType.Monster)
            .Select(e => new FlightTarget(ecs.Get<ActorComponent>(e).Identifier, ecs.Get<WorldComponent>(e).Position, ecs.Get<MonsterCombatComponent>(e).BodyRadius, ecs.Get<MonsterCombatComponent>(e).BodyHeight))
            .ToArray();
    }

    public SectorKillProgress GetKillProgress(int sector)
    {
        if (!KillProgress.TryGetValue(sector, out var progress))
        {
            var box = batch.EventBox.Sectors.FirstOrDefault(e => e.Id == sector);

            progress = box is null ? new SectorKillProgress(ClearType.None, 0, []) : new SectorKillProgress(box);

            KillProgress.Add(sector, progress);
        }

        return progress;
    }

    /// <summary>
    /// Boxes of a sector that have not appeared yet and whose monsters have to die with the rest.
    /// </summary>
    public int CountBoxesToCome(int sector)
    {
        return batch.EventBox.MonsterSpawns.Count(e => e.Sector == sector && !SpawnedBoxes.Contains(e.Id) && HoldsSector(e));
    }

    private bool HoldsSector(MonsterSpawnBox box)
    {
        return box.MonsterList
            .TakeWhile(e => e.Id is not 0)
            .All(e => e.Type is not MonsterSpawnType.Npc && tables.HoldsSector(e.Id));
    }

    public void Remove(Entity creature)
    {
        if (ecs.Get<ActorComponent>(creature).Type == ActorType.Monster && ecs.Get<MonsterCombatComponent>(creature).HoldsSector)
        {
            GetKillProgress(ecs.Get<NonPlayableCreatureComponent>(creature).Sector).Removed();
        }

        Bodies.Remove(creature);
        Defenses.Remove(creature);
        Creatures.Remove(creature);
        Scenery.Remove(creature);

        ecs.Destroy(creature);
    }

    public void Close()
    {
        Closed = true;

        foreach (var creature in Creatures.Where(ecs.IsAlive))
        {
            ecs.Destroy(creature);
        }

        Creatures.Clear();
        Scenery.Clear();
        Bodies.Clear();
        Defenses.Clear();
        Players.Clear();
    }

    private async Task ShowLaterAsync(ServerSessionComponent session, List<Entity> created, TimeSpan delay)
    {
        try
        {
            await Task.Delay(delay);

            if (!Closed)
            {
                Show(session, created.Where(ecs.IsAlive).ToList());
            }
        }

        catch (Exception e)
        {
            Console.WriteLine($"[{nameof(ShowLaterAsync)}]: {e}");
        }
    }

    public void MoveToPoint(int npc, int waypoint)
    {
        var creature = Creatures.FirstOrDefault(x => ecs.Get<CreatureComponent>(x) == npc);

        if (creature.IsAlive() is false)
        {
            Console.WriteLine($"[{nameof(MoveToPoint)}]: creature not found - {npc}");
        }
    }

    // ---- Minds --------------------------------------------------------------------------------

    private void StartTicking()
    {
        if (!Ticking)
        {
            Ticking = true;

            _ = TickAsync();
        }
    }

    /// <summary>
    /// The monsters think on the thread of the world, one after another, so nothing they do can meet another change halfway.
    /// </summary>
    private async Task TickAsync()
    {
        var last = Stopwatch.GetTimestamp();

        while (!Closed)
        {
            try
            {
                await Task.Delay(TickInterval);
            }

            catch (OperationCanceledException)
            {
                return;
            }

            if (Closed)
            {
                return;
            }

            var elapsed = (float)Stopwatch.GetElapsedTime(last).TotalSeconds;

            last = Stopwatch.GetTimestamp();

            foreach (var body in Bodies.Values.ToArray())
            {
                try
                {
                    RestoreSuperArmor(body.Entity);

                    body.Update(Now, elapsed);
                }

                catch (Exception e)
                {
                    Logger?.LogError(e, "The monster {Monster} failed to think", body.Behavior.Id);
                }
            }
        }
    }

    /// <summary>
    /// A player has hurt a monster: the monster remembers who did it, and the more damage, the more it cares.
    /// </summary>
    public void OnMonsterHit(Entity monster, Entity attacker, int damage)
    {
        if (Bodies.TryGetValue(monster, out var body) && ecs.IsAlive(attacker))
        {
            body.Machine.OnDamaged(ecs.Get<ActorComponent>(attacker), damage * AggroPerDamage);
        }
    }

    private const float AggroPerDamage = 0.0004f;

    /// <summary>
    /// How much a monster with super armor can take before it breaks, as a share of its health. The tables of this client
    /// do not say, so this is a guess.
    /// </summary>
    private const float SuperArmorShare = 0.2f;

    /// <summary>
    /// How long a broken super armor stays broken, in milliseconds.
    /// </summary>
    private const long SuperArmorRecovery = 3000;

    /// <summary>
    /// What a hit does to the monster that takes it, as the clients are told in the hit.
    /// </summary>
    public readonly record struct HitEffect(byte Reaction, byte Defense, float SuperArmor);

    /// <summary>
    /// A monster takes a hit on its super armor. As long as the armor holds it does not flinch; when it breaks it does,
    /// and it is helpless until the armor comes back.
    /// </summary>
    public HitEffect AbsorbHit(Entity monster, float armorDamage, int reaction, bool survives)
    {
        var combat = ecs.Get<MonsterCombatComponent>(monster);

        if (combat.MaxSuperArmor <= 0f)
        {
            return new HitEffect(Flinch(monster, reaction, survives), (byte)combat.Defense, 0f);
        }

        var now = Environment.TickCount64;

        if (combat.SuperArmorBrokenUntil > now)
        {
            return new HitEffect(Flinch(monster, reaction, survives), (byte)DefenseType.Normal, 0f);
        }

        var gauge = Math.Max(0f, combat.SuperArmor - armorDamage);

        if (gauge > 0f)
        {
            ecs.Set(monster, combat with { SuperArmor = gauge });
            Broadcast(new MonsterSuperArmorGageResponse { Actor = ecs.Get<ActorComponent>(monster), Current = gauge, Max = combat.MaxSuperArmor });

            return new HitEffect(0, (byte)DefenseType.SuperArmor, gauge);
        }

        ecs.Set(monster, combat with { SuperArmor = 0f, SuperArmorBrokenUntil = now + SuperArmorRecovery });
        Broadcast(new MonsterSuperArmorGageResponse { Actor = ecs.Get<ActorComponent>(monster), Current = 0f, Max = combat.MaxSuperArmor });

        return new HitEffect(Flinch(monster, reaction, survives), (byte)DefenseType.Normal, 0f);
    }

    private byte Flinch(Entity monster, int reaction, bool survives)
    {
        if (reaction != 0 && survives && Bodies.TryGetValue(monster, out var body))
        {
            body.Stagger(FlinchSeconds);
        }

        return (byte)reaction;
    }

    private const float FlinchSeconds = 0.6f;

    /// <summary>
    /// Armor that has had time to recover is whole again.
    /// </summary>
    private void RestoreSuperArmor(Entity monster)
    {
        var combat = ecs.Get<MonsterCombatComponent>(monster);

        if (combat.SuperArmorBrokenUntil == 0 || combat.SuperArmorBrokenUntil > Environment.TickCount64)
        {
            return;
        }

        ecs.Set(monster, combat with { SuperArmor = combat.MaxSuperArmor, SuperArmorBrokenUntil = 0 });
        Broadcast(new MonsterSuperArmorGageResponse { Actor = ecs.Get<ActorComponent>(monster), Current = combat.MaxSuperArmor, Max = combat.MaxSuperArmor });
    }

    internal void SetInvincible(Entity monster, bool invincible)
    {
        if (!ecs.IsAlive(monster))
        {
            return;
        }

        var combat = ecs.Get<MonsterCombatComponent>(monster);

        if (invincible)
        {
            Defenses.TryAdd(monster, combat.Defense);
            ecs.Set(monster, combat with { Defense = DefenseType.Invincible });
        }
        else if (Defenses.Remove(monster, out var defense))
        {
            ecs.Set(monster, combat with { Defense = defense });
        }
    }

    /// <summary>
    /// A monster that ends its own life. The progress of its sector is the business of the maze, so it only stops being.
    /// </summary>
    internal void Kill(Entity monster)
    {
        Logger?.LogDebug("Monster {Monster} wants to die, which a maze script has to finish", monster);
    }

    // ---- Players ------------------------------------------------------------------------------

    private const ushort ReviveNotFallen = 52608;
    private const ushort ReviveNotAllowed = 51009;

    /// <summary>
    /// A fallen player gets up where they fell, with all their life and a few seconds in which nothing can hurt them.
    /// The only ways to get up in a maze are free: the others need a buff, a safe place of a district or cash.
    /// </summary>
    public void Revive(Entity player, ReviveType type)
    {
        if (!ecs.IsAlive(player) || !ecs.Has<ServerSessionComponent>(player))
        {
            return;
        }

        var session = ecs.Get<ServerSessionComponent>(player);

        if (Vitals.IsAlive(player))
        {
            session.Send(new ErrorMessageResponse(new MessageOpcode(GroupOpcode.Character, CharacterOpcode.ReviveRes), ReviveNotFallen));

            return;
        }

        if (type is not (ReviveType.Auto or ReviveType.MazeFree or ReviveType.ModeMazeAuto or ReviveType.Cash))
        {
            session.Send(new ErrorMessageResponse(new MessageOpcode(GroupOpcode.Character, CharacterOpcode.ReviveRes), ReviveNotAllowed));

            return;
        }

        Vitals.Restore(player);
        Vitals.Protect(player);

        var actor = ecs.Get<ActorComponent>(player);
        var place = ecs.Get<WorldComponent>(player);

        Broadcast(new PersonUpdateStatListResponse
        {
            Type = 1,
            Actor = actor,
            Stats = [new ST_STAT { fValue = Vitals.MaxHealth(player), wStatID = (short)STAT_TYPE.STAT_HP_CUR }]
        });

        Broadcast(new PersonReviveResponse
        {
            Actor = actor,
            Owner = 0,
            Type = (byte)type,
            Position = place.Position,
            Rotation = place.Rotation
        });
    }

    internal void Broadcast(IResponseHotspotMessage message)
    {
        foreach (var player in Players)
        {
            if (ecs.IsAlive(player) && ecs.Has<ServerSessionComponent>(player))
            {
                ecs.Get<ServerSessionComponent>(player).Send(message);
            }
        }
    }

    private AiTarget Describe(Entity player)
    {
        var place = ecs.Get<WorldComponent>(player);

        return new AiTarget(ecs.Get<ActorComponent>(player), place.Position, PlayerVitals.BodyRadius, place.Rotation, Vitals.Health(player), Vitals.MaxHealth(player), false);
    }

    /// <summary>
    /// The players as something a projectile can run into, named by the actor.
    /// </summary>
    internal FlightTarget[] GetPlayerTargets()
    {
        return Players
            .Where(e => ecs.IsAlive(e) && Vitals.IsAlive(e))
            .Select(e => new FlightTarget(ecs.Get<ActorComponent>(e).Identifier, Vitals.PositionOf(e), PlayerVitals.BodyRadius, PlayerVitals.BodyHeight))
            .ToArray();
    }

    internal Entity? FindPlayerEntity(int identifier)
    {
        foreach (var player in Players)
        {
            if (ecs.IsAlive(player) && Vitals.IsAlive(player) && ecs.Get<ActorComponent>(player).Identifier == identifier)
            {
                return player;
            }
        }

        return null;
    }

    internal AiTarget? FindPlayer(int id)
    {
        foreach (var player in Players)
        {
            if (ecs.IsAlive(player) && Vitals.IsAlive(player) && (int)ecs.Get<ActorComponent>(player) == id)
            {
                return Describe(player);
            }
        }

        return null;
    }

    internal AiTarget? NearestPlayer(Vector3 from, float distance)
    {
        AiTarget? best = null;
        var nearest = distance;

        foreach (var player in Players.Where(e => ecs.IsAlive(e) && Vitals.IsAlive(e)))
        {
            var target = Describe(player);
            var between = Vector3.Distance(from, target.Position) - target.Radius;

            if (between <= nearest && Navigation.CanSee(from, target.Position))
            {
                nearest = between;
                best = target;
            }
        }

        return best;
    }

    internal int CountPlayers(Vector3 from, float distance)
    {
        return Players.Count(e => ecs.IsAlive(e) && Vitals.IsAlive(e) && Vector3.Distance(from, Vitals.PositionOf(e)) <= distance);
    }

    internal int CountMonsters(int sector)
    {
        return Creatures.Count(e => ecs.Get<ActorComponent>(e).Type == ActorType.Monster && ecs.Get<NonPlayableCreatureComponent>(e).Sector == sector);
    }

    internal IReadOnlyList<Entity> PlayersTouching(AttackArea area)
    {
        return Players
            .Where(e => ecs.IsAlive(e) && Vitals.IsAlive(e) && area.Touches(Vitals.PositionOf(e), PlayerVitals.BodyRadius, PlayerVitals.BodyHeight))
            .ToArray();
    }

    /// <summary>
    /// A monster has hit the players in the area of an attack: tells everyone how hard, and who has fallen.
    /// </summary>
    internal void HitPlayers(MonsterBody attacker, int skillId, AttackTrigger trigger, IReadOnlyList<Entity> victims)
    {
        if (victims.Count == 0 || !trigger.AffectsTargets || !ecs.IsAlive(attacker.Entity))
        {
            return;
        }

        var stats = ecs.Get<MonsterCombatComponent>(attacker.Entity).Stats;
        var skill = tables.GetSkill(skillId);
        var hits = new List<SkillDmgValue>();
        var fallen = new List<Entity>();

        foreach (var victim in victims)
        {
            if (!Vitals.IsAlive(victim))
            {
                continue;
            }

            var shielded = Vitals.IsProtected(victim);
            var result = shielded
                ? new DamageResult(0, DamageFlags.None)
                : DamageCalculator.Calculate(stats, Vitals.StatsOf(victim), skill, DefenseType.Normal, trigger.DamageRate, Random);
            var left = Vitals.Hurt(victim, result.Damage);

            hits.Add(default(SkillDmgValue) with
            {
                UxActorId = ecs.Get<ActorComponent>(victim),
                ReactionType = shielded ? (byte)0 : (byte)trigger.ReactionType,
                DamageFlags = (byte)result.Flags,
                Damage = result.Damage,
                Health = left,
                DefenseType = (byte)(shielded ? DefenseType.Invincible : DefenseType.Normal)
            });

            if (left == 0)
            {
                fallen.Add(victim);
            }
        }

        if (hits.Count == 0)
        {
            return;
        }

        var place = ecs.Get<WorldComponent>(attacker.Entity);

        Broadcast(new SkillActionBroadcastResponse
        {
            SkillDmgValueList = hits.ToArray(),
            SkillAction = default(SkillActionValue) with
            {
                SkillActorInfo = default(SkillActorInfoValue) with
                {
                    UxActorId = ecs.Get<ActorComponent>(attacker.Entity),
                    SkillPosInfo = default(SkillActionPosInfoValue) with
                    {
                        Position = place.Position,
                        Angle = place.Rotation,
                        MotionClass = -1
                    }
                },
                Skill = skillId,
                TriggerIndex = (short)trigger.Id,
                ContinuousHit = 1
            }
        });

        foreach (var victim in fallen)
        {
            Broadcast(new PersonDieResponse
            {
                Victim = ecs.Get<ActorComponent>(victim),
                Attacker = default,
                PvpKillCount = 0
            });
        }
    }
}
