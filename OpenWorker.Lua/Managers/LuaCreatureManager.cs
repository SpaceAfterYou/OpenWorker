using System.Diagnostics;
using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
using OpenWorker.Hotspot.Modules.Movement.Responses;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Hotspot.Modules.World.Types;
using OpenWorker.Lua.Ai;
using OpenWorker.Lua.Buffs;
using OpenWorker.Lua.Helpers;
using OpenWorker.Navigation;

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

    private readonly World ecs;
    private readonly VBatchFile batch;
    private readonly CombatTables tables;

    public LuaCreatureManager(World ecs, VBatchFile batch, CombatTables tables, CreatureServices? services = null, ILogger? logger = null, NavMesh? mesh = null)
    {
        this.ecs = ecs;
        this.batch = batch;
        this.tables = tables;

        Services = services;
        Navigation = mesh is null ? OpenNavigation.Instance : new NavMeshNavigation(mesh, batch);
        Vitals = new PlayerVitals(ecs, tables);
        Logger = logger ?? NullLogger.Instance;
        Helpers = new HelperSquad(ecs, this, tables);
    }

    /// <summary>
    /// The helpers the players have called into the maze.
    /// </summary>
    public HelperSquad Helpers { get; }

    /// <summary>
    /// A new actor of the maze: a helper is one too.
    /// </summary>
    internal int NextActor() => Offset++;

    /// <summary>
    /// Every creature of the maze, the townsfolk with the monsters.
    /// </summary>
    internal IReadOnlyList<Entity> Living => Creatures;

    /// <summary>
    /// The clients have been shown the creature: it is not waiting for its delay.
    /// </summary>
    internal bool IsSeen(Entity creature) => !Unseen.Contains(creature);

    internal CreatureServices? Services { get; }

    /// <summary>
    /// Where the creatures that appear later wait, the group of the maze that owns them. Without one they appear at once.
    /// </summary>
    public ScheduleGroup? Schedule { get; set; }

    internal ILogger Logger { get; }

    /// <summary>
    /// How much life the players of the maze have and who can be hurt.
    /// </summary>
    public PlayerVitals Vitals { get; }

    /// <summary>
    /// The buffs of the players and monsters of the maze; none in a maze made without them.
    /// </summary>
    public MazeBuffs? Buffs { get; internal set; }

    /// <summary>
    /// Where a monster can walk.
    /// </summary>
    public INavigation Navigation { get; set; }

    /// <summary>
    /// A gate has been opened or closed, which opens or closes the way for the monsters.
    /// </summary>
    public void SetGate(int gate, bool open)
    {
        (Navigation as NavMeshNavigation)?.SetGate(gate, open);
    }

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

    private List<Entity> Creatures { get; } = [];
    private List<Entity> Players { get; } = [];

    /// <summary>
    /// Creatures that wait for their delay before the clients see them.
    /// </summary>
    private HashSet<Entity> Unseen { get; } = [];
    private Dictionary<Entity, MonsterBody> Bodies { get; } = [];

    /// <summary>
    /// The bodies in the order they think, walked in place by <see cref="Step"/>.
    /// </summary>
    private StepList<MonsterBody> Minds { get; } = new();

    /// <summary>
    /// Bodies of monsters that are gone, kept to give a mind to the next monster of the same script.
    /// </summary>
    private Dictionary<AiProfile, Stack<MonsterBody>> SpareBodies { get; } = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// Spare bodies of one script kept at most: a maze has a few kinds of monster and spawns them in waves.
    /// </summary>
    private const int MaxSpareBodies = 32;
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

    /// <summary>
    /// A player who has left the maze: the monsters forget them and their client hears no more of the maze.
    /// </summary>
    public void Detach(Entity player)
    {
        Players.Remove(player);
        Vitals.Forget(player);
    }

    /// <summary>
    /// The players in the maze, in the order they came.
    /// </summary>
    public IReadOnlyList<Entity> Members => Players;

    public void Create(int box)
    {
        // Maps can point at boxes they no longer have, and a client walks through such a trigger like any other.
        if (batch.EventBox.MonsterSpawns.Find(box) is not { } spawn)
        {
            return;
        }

        SpawnedBoxes.Add(box);

        Logger.SpawnBox(box, spawn.LayerBitmask);

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
                    Logger.UnsupportedMonsterType(box, monster.Id, monster.Type);
                    break;
            }
        }

        switch (spawn.CreationCondition)
        {
            case CreationConditionType.Immediate:
                Show(created);
                break;

            case CreationConditionType.WaitSignal:
                ShowLater(created, TimeSpan.FromSeconds(float.IsFinite(spawn.WaitCreationDelayTime) ? Math.Clamp(spawn.WaitCreationDelayTime, 0f, 86_400f) : 0f));
                break;
        }
    }

    private void Show(List<Entity> created)
    {
        var npcs = created.Where(e => ecs.Get<ActorComponent>(e).Type == ActorType.Npc).ToArray();
        var monsters = created.Except(npcs).ToArray();

        if (monsters.Length > 0)
        {
            Broadcast(DescribeMonsters(monsters));
        }

        if (npcs.Length > 0)
        {
            Broadcast(new WorldOtherInfosNpcResponse { List = npcs.Select(ToNpcEntry).ToArray() });
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

    private WorldOtherInfosMonsterResponse DescribeMonsters(Entity[] monsters)
    {
        return new WorldOtherInfosMonsterResponse
        {
            Monsters = monsters.Select(e => MonsterSnapshotMapper.ToStMonsterInfo(ecs, e, Bodies.TryGetValue(e, out var body) ? body.IsBattlePose : true)).ToArray()
        };
    }

    /// <summary>
    /// Tells a player who comes into a maze that runs what lives in it now: the monsters and the townsfolk the others see.
    /// </summary>
    public void ShowAllTo(ServerSessionComponent session)
    {
        var seen = Creatures.Where(e => !Unseen.Contains(e) && ecs.IsAlive(e)).ToArray();
        var npcs = seen.Where(e => ecs.Get<ActorComponent>(e).Type == ActorType.Npc).ToArray();
        var monsters = seen.Except(npcs).ToArray();

        if (monsters.Length > 0)
        {
            session.Send(DescribeMonsters(monsters));
        }

        if (npcs.Length > 0)
        {
            session.Send(new WorldOtherInfosNpcResponse { List = npcs.Select(ToNpcEntry).ToArray() });
        }

        Buffs?.ShowTo(session);
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
        ecs.Set(creature, new WorldComponent { Location = 0, Position = OnFloor(spawn.GetPosition()), Rotation = spawn.Rotation, Jump = 0 });
    }

    /// <summary>
    /// A box says where in it a creature appears, not how high: the creature stands on the floor under that place.
    /// </summary>
    private Vector3 OnFloor(Vector3 position)
    {
        return Navigation.TryGetFloor(position, out var height) ? position with { Z = height } : position;
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

        GiveMind(creature, identifier, ecs.Get<WorldComponent>(creature).Position);

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

        var body = SpareBodies.TryGetValue(profile, out var spare) && spare.TryPop(out var used)
            ? used.Revive(creature, behavior, home)
            : new MonsterBody(ecs, this, services.Actions, creature, behavior, profile, home);

        Bodies.Add(creature, body);
        Minds.Add(body);
    }

    /// <summary>
    /// The mind of a monster that is gone waits for the next monster of its script.
    /// </summary>
    private void Retire(MonsterBody body)
    {
        Minds.Remove(body);

        if (!SpareBodies.TryGetValue(body.Machine.Profile, out var spare))
        {
            spare = new Stack<MonsterBody>();
            SpareBodies.Add(body.Machine.Profile, spare);
        }

        if (spare.Count < MaxSpareBodies)
        {
            spare.Push(body);
        }
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
    public void SpawnOnce(int box)
    {
        if (SpawnedBoxes.Add(box))
        {
            Create(box);
        }
    }

    /// <summary>
    /// A box that has not appeared yet never will.
    /// </summary>
    public void Terminate(int box)
    {
        SpawnedBoxes.Add(box);
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
            var box = batch.EventBox.Sectors.Find(sector);

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
        return batch.EventBox.SpawnsOf(sector).Count(e => !SpawnedBoxes.Contains(e.Id) && HoldsSector(e));
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

        if (Bodies.Remove(creature, out var body))
        {
            Retire(body);
        }

        Defenses.Remove(creature);
        Creatures.Remove(creature);
        Scenery.Remove(creature);
        Unseen.Remove(creature);
        Buffs?.Forget(creature);

        ecs.Destroy(creature);
    }

    public void Close()
    {
        Closed = true;

        foreach (var creature in Creatures)
        {
            if (ecs.IsAlive(creature))
            {
                ecs.Destroy(creature);
            }
        }

        Creatures.Clear();
        Scenery.Clear();
        Bodies.Clear();
        Minds.Clear();
        SpareBodies.Clear();
        Defenses.Clear();
        Players.Clear();
        Unseen.Clear();
        Helpers.Clear();
    }

    /// <summary>
    /// The creatures of a box that waits for a signal show themselves after its delay, if the maze still runs.
    /// </summary>
    private void ShowLater(List<Entity> created, TimeSpan delay)
    {
        if (Schedule is not { } schedule)
        {
            Show(created);

            return;
        }

        Unseen.UnionWith(created);

        schedule.Schedule(delay, () =>
        {
            if (!Closed)
            {
                Unseen.ExceptWith(created);

                Show(created.Where(ecs.IsAlive).ToList());
            }
        });
    }

    /// <summary>
    /// How fast townsfolk walk, in units a second: the pace of a walking hero, as the tables give townsfolk no speed of their own.
    /// </summary>
    public const float NpcWalkSpeed = 300f;

    /// <summary>
    /// The creature of a table id that the maze holds, the first one when there are several.
    /// </summary>
    public bool TryFindCreature(int identifier, out Entity found)
    {
        // Asking a default entity whether it lives reads the world of id 0, which another maze may have destroyed.
        foreach (var creature in Creatures)
        {
            if (ecs.IsAlive(creature) && ecs.Get<CreatureComponent>(creature) == identifier)
            {
                found = creature;
                return true;
            }
        }

        found = Entity.Null;
        return false;
    }

    /// <summary>
    /// A townsman sets off towards a place: the client sees it walk there. The time the walk takes comes back, in seconds.
    /// </summary>
    public float Walk(Entity npc, Vector3 target)
    {
        var place = ecs.Get<WorldComponent>(npc);
        var distance = Vector2.Distance(new Vector2(place.Position.X, place.Position.Y), new Vector2(target.X, target.Y));
        var yaw = distance > 0f ? AiMath.YawTo(place.Position, target) : place.Rotation;

        ecs.Set(npc, place with { Rotation = yaw });

        Broadcast(new MovementMoveBtResponse
        {
            Actor = ecs.Get<ActorComponent>(npc),
            Map = place.Map,
            Pos = place.Position,
            Yaw = yaw,
            TargetPosX = target.X,
            TargetPosY = target.Y,
            RunBit = 0,
            Pitch = 0f,
            MoveSpeed = NpcWalkSpeed,
            ChangeMotion = 0
        });

        return distance / NpcWalkSpeed;
    }

    /// <summary>
    /// A townsman turns where it stands, and the client sees it.
    /// </summary>
    public void Turn(Entity npc, float yaw)
    {
        ecs.Set(npc, ecs.Get<WorldComponent>(npc) with { Rotation = yaw });

        Broadcast(new MovementUpdateDirResponse { Actor = ecs.Get<ActorComponent>(npc), Yaw = yaw, Pitch = 0f, Direct = false });
    }

    /// <summary>
    /// A townsman has arrived: it stands at the place, and the client sees it stop there.
    /// </summary>
    public void Arrive(Entity npc, Vector3 target)
    {
        var place = ecs.Get<WorldComponent>(npc) with { Position = target };

        ecs.Set(npc, place);

        Broadcast(new MovementStopBtResponse
        {
            Actor = ecs.Get<ActorComponent>(npc),
            MapId = place.Map,
            PosX = target.X,
            PosY = target.Y,
            PosZ = target.Z,
            Yaw = place.Rotation,
            Pitch = 0f
        });
    }

    // ---- Minds --------------------------------------------------------------------------------

    /// <summary>
    /// The monsters think on the thread of the world, one after another, so nothing they do can meet another change
    /// halfway. The maze calls it at every step of the clock. A monster that goes during the step is skipped, one that
    /// comes thinks from the next step on.
    /// </summary>
    public void Step(TimeSpan elapsed)
    {
        if (Closed)
        {
            return;
        }

        var seconds = (float)elapsed.TotalSeconds;
        var now = Now;
        var count = Minds.BeginStep();

        try
        {
            for (var index = 0; index < count && !Closed; index++)
            {
                if (Minds.At(index) is not { } body)
                {
                    continue;
                }

                try
                {
                    RestoreSuperArmor(body.Entity);

                    // A monster that is stunned, asleep, paralysed or frozen does nothing until it is over.
                    if (Buffs?.IsHeld(body.Entity) == true)
                    {
                        continue;
                    }

                    body.Update(now, seconds);
                }

                catch (Exception e)
                {
                    Logger.MonsterFailedToThink(e, body.Behavior.Id);
                }
            }
        }

        finally
        {
            Minds.EndStep();
        }

        Helpers.Step(now, seconds);
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
    /// A buff holds a monster: it stops where it stands and drops the attack it was making.
    /// </summary>
    internal void Hold(Entity monster)
    {
        if (Bodies.TryGetValue(monster, out var body))
        {
            body.Stagger(0f);
        }

        Helpers.Hold(monster);
    }

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
        Logger.MonsterWantsToDie(monster.Id);
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
            session.Send(new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.Character, CharacterOpcode.ReviveRes), ReviveNotFallen));

            return;
        }

        if (type is not (ReviveType.Auto or ReviveType.MazeFree or ReviveType.ModeMazeAuto or ReviveType.Cash))
        {
            session.Send(new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.Character, CharacterOpcode.ReviveRes), ReviveNotAllowed));

            return;
        }

        Vitals.Restore(player);
        Vitals.Protect(player);

        // Coming back fills the gauges as it fills the life.
        Services?.Gauges?.Fill(player);

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

    /// <summary>
    /// Tells every player of the maze, written once for all of them.
    /// </summary>
    public void Broadcast(IResponseHotspotMessage message)
    {
        if (Players.Count == 0)
        {
            return;
        }

        using var frame = Frame.Encode(message);

        Broadcast(frame, Entity.Null);
    }

    /// <summary>
    /// Tells every player of the maze but one.
    /// </summary>
    public void BroadcastToOthers(Entity except, IResponseHotspotMessage message)
    {
        if (Players.Count < 2)
        {
            return;
        }

        using var frame = Frame.Encode(message);

        Broadcast(frame, except);
    }

    /// <summary>
    /// Sends a message written already to every player of the maze but <paramref name="except"/>.
    /// </summary>
    public void Broadcast(Frame? frame, Entity except)
    {
        if (frame is null)
        {
            return;
        }

        foreach (var player in Players)
        {
            if (player != except && ecs.IsAlive(player) && ecs.Has<ServerSessionComponent>(player))
            {
                ecs.Get<ServerSessionComponent>(player).Send(frame);
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
        // An attack for the side of the monster, or one that only gives a buff, hurts nobody.
        if (trigger.ForAllies || (trigger.Buff > 0 && trigger.DamageRate <= 0f))
        {
            Buffs?.GiveFromAttack(attacker.Entity, trigger, victims);

            return;
        }

        if (victims.Count == 0 || !trigger.AffectsTargets || !ecs.IsAlive(attacker.Entity))
        {
            return;
        }

        var stats = Buffs?.StatsOf(attacker.Entity, ecs.Get<MonsterCombatComponent>(attacker.Entity).Stats) ?? ecs.Get<MonsterCombatComponent>(attacker.Entity).Stats;
        var skill = tables.GetSkill(skillId);
        var hits = new List<SkillDmgValue>();
        var fallen = new List<Entity>();
        List<Entity>? hurt = null;

        foreach (var victim in victims)
        {
            if (!Vitals.IsAlive(victim))
            {
                continue;
            }

            var defense = Buffs?.DefenseOf(victim, DefenseType.Normal) ?? DefenseType.Normal;
            var shielded = Vitals.IsProtected(victim) || defense == DefenseType.Invincible;
            var result = shielded
                ? new DamageResult(0, HitResults.None)
                : DamageCalculator.Calculate(stats, Vitals.StatsOf(victim), skill, defense, trigger.DamageRate, Random);
            var left = Vitals.Hurt(victim, result.Damage);

            hits.Add(default(SkillDmgValue) with
            {
                UxActorId = ecs.Get<ActorComponent>(victim),
                ReactionType = shielded ? (byte)0 : (byte)trigger.ReactionType,
                DamageFlags = (byte)result.Flags,
                Damage = result.Damage,
                Health = left,
                DefenseType = (byte)(shielded ? DefenseType.Invincible : defense)
            });

            if (left == 0)
            {
                fallen.Add(victim);
            }
            else if (result.Damage > 0)
            {
                (hurt ??= []).Add(victim);
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

        // A blow that lands may wear the armour of the player, a fall the weapon and the armour.
        foreach (var victim in hurt ?? [])
        {
            Services?.Wear?.Hit(victim);
        }

        foreach (var victim in fallen)
        {
            Broadcast(new PersonDieResponse
            {
                Victim = ecs.Get<ActorComponent>(victim),
                Attacker = default,
                PvpKillCount = 0
            });

            Buffs?.OnDeath(victim);
            Services?.Wear?.Died(victim);
        }

        Buffs?.GiveFromHit(attacker.Entity, trigger, victims);
    }
}
