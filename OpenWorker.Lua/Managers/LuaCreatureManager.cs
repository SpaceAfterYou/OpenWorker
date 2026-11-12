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
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Maze;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Maze.Responses;
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
using OpenWorker.Wire;

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
    /// The players a monster holds in a grab: no other monster takes them.
    /// </summary>
    private HashSet<Entity> Grabbed { get; } = [];

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
        Grabbed.Remove(player);
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
            CreateOf(spawn, monster, created);
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

    /// <summary>
    /// Makes what one slot of a box holds, by the kind it is.
    /// </summary>
    private void CreateOf(MonsterSpawnBox spawn, Monster monster, List<Entity> created)
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
                Logger.UnsupportedMonsterType(spawn.Id, monster.Id, monster.Type);
                break;
        }
    }

    /// <summary>
    /// One wave of a box that waits for a signal: each of the slots makes as many creatures as the box allows at once, and the
    /// clients see them at once. The box counts as appeared only when <paramref name="last"/> says this is its last wave, so a
    /// sector that waits for the box is not cleared between two waves.
    /// </summary>
    public IReadOnlyList<Entity> SpawnWave(MonsterSpawnBox spawn, IReadOnlyList<int> slots, bool last)
    {
        var created = new List<Entity>();

        foreach (var slot in slots)
        {
            if (slot is < 0 or >= 10 || slot >= spawn.MonsterList.Count || spawn.MonsterList[slot].Id is 0)
            {
                continue;
            }

            for (var count = 0; count < spawn.MaxEntityCount; count++)
            {
                CreateOf(spawn, spawn.MonsterList[slot], created);
            }
        }

        if (last)
        {
            SpawnedBoxes.Add(spawn.Id);
        }

        if (created.Count > 0)
        {
            Show(created);
        }

        return created;
    }

    private void Show(List<Entity> created)
    {
        var npcs = created.Where(e => ecs.Get<ActorComponent>(e).Type == ActorType.Npc).ToArray();
        var monsters = created.Except(npcs).ToArray();

        if (monsters.Length > 0)
        {
            Broadcast(DescribeMonsters(monsters));

            // The monsters that come with an animation of their own play it where they appear.
            var idles = monsters
                .Select(e => Bodies.TryGetValue(e, out var body) ? body.StartSpawnAction() : null)
                .OfType<MovementIdleEntry>()
                .ToArray();

            if (idles.Length > 0)
            {
                Broadcast(new MovementIdlesBtResponse { Entries = idles });
            }
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

            var idles = monsters
                .Select(e => Bodies.TryGetValue(e, out var body) ? body.CurrentAction() : null)
                .OfType<MovementIdleEntry>()
                .ToArray();

            if (idles.Length > 0)
            {
                session.Send(new MovementIdlesBtResponse { Entries = idles });
            }
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

        var combat = Strength(identifier, PartyCount);

        ecs.Set(creature, combat);
        ecs.Set(creature, ecs.Get<CreatureComponent>(creature) with { Level = combat.Stats.Level, Health = (int)combat.Stats.MaxHealth });

        if (combat.HoldsSector)
        {
            GetKillProgress(spawn.Sector).Added();
        }

        Creatures.Add(creature);

        GiveMind(creature, identifier, ecs.Get<WorldComponent>(creature).Position, spawn);

        return creature;
    }

    /// <summary>
    /// How many members the party of the maze has, which decides how strong its monsters are. A maze of one player counts one.
    /// </summary>
    public int PartyCount { get; private set; } = 1;

    /// <summary>
    /// The maze is opened for a party of <paramref name="members"/>: the monsters that appear are made for that many.
    /// </summary>
    public void StartParty(int members)
    {
        PartyCount = Math.Max(1, members);
    }

    /// <summary>
    /// The monster as strong as it is for the number of members: what its group says when the tables have a row for it, else
    /// what its own table gives and a guessed gauge for its super armor.
    /// </summary>
    private MonsterCombatComponent Strength(int identifier, int members)
    {
        var combat = tables.GetMonster(identifier);

        if (Services?.Revision is { } revision && revision.TryGet(identifier, members, out _))
        {
            return revision.Apply(combat, identifier, members);
        }

        if (combat.Defense == DefenseType.SuperArmor)
        {
            var gauge = combat.Stats.MaxHealth * SuperArmorShare;

            combat = combat with { SuperArmor = gauge, MaxSuperArmor = gauge };
        }

        return combat;
    }

    /// <summary>
    /// The party of the maze has <paramref name="members"/> members now, which it has when a member leaves it. The monsters
    /// that are alive become as strong as that many members make them, each with the same share of its life and of its super
    /// armor as before; a monster of a kind other than the plain one is left as it is. The members are told of the change.
    /// </summary>
    public void ResizeParty(int members)
    {
        PartyCount = Math.Max(1, members);

        if (Services?.Revision is not { } revision)
        {
            return;
        }

        foreach (var creature in Creatures.ToArray())
        {
            if (!ecs.IsAlive(creature) || ecs.Get<ActorComponent>(creature).Type != ActorType.Monster)
            {
                continue;
            }

            var state = ecs.Get<CreatureComponent>(creature);

            if (!revision.IsRevised(state.Identifier))
            {
                continue;
            }

            Restrengthen(creature, state, revision);
        }

        Broadcast(new MazeNoticeResponse { Type = PartyStrengthNotice });
    }

    /// <summary>
    /// The notice the clients show when the monsters change their strength because the party has changed.
    /// </summary>
    private const int PartyStrengthNotice = 80;

    private void Restrengthen(Entity creature, CreatureComponent state, PartyRevision revision)
    {
        var before = ecs.Get<MonsterCombatComponent>(creature);
        var after = revision.Apply(tables.GetMonster(state.Identifier), state.Identifier, PartyCount);

        // The share of life the monster has left stays, and so does the share of its super armor.
        var share = state.Health > 0 && before.Stats.MaxHealth > 0f ? state.Health / before.Stats.MaxHealth : 1f;
        var armorShare = before.SuperArmor > 0f && before.MaxSuperArmor > 0f ? before.SuperArmor / before.MaxSuperArmor : 1f;
        var health = state.Health > 0 ? Math.Max(1, (int)(after.Stats.MaxHealth * share)) : 0;

        after = after with { SuperArmor = (int)(after.MaxSuperArmor * armorShare), LastCountedHit = before.LastCountedHit };

        // What the fight did to the monster is not in its table: an armor that is broken stays broken until its time is up,
        // then comes back whole at the new gauge, and a defense its mind forced on it (an escape, the way home) stays until the
        // mind gives the monster its own back, which is the one of the new table.
        if (before.SuperArmorBrokenUntil != 0)
        {
            after = after with { SuperArmor = 0f, SuperArmorBrokenUntil = before.SuperArmorBrokenUntil };
        }

        if (Defenses.ContainsKey(creature))
        {
            Defenses[creature] = after.Defense;
            after = after with { Defense = before.Defense };
        }

        ecs.Set(creature, after);
        ecs.Set(creature, state with { Health = health });

        var changes = CombatStats.Differences(before.Stats, after.Stats)
            .Select(e => new StatKeyValuePair { byIndex = e.Type, statValue = e.Value })
            .Append(new StatKeyValuePair { byIndex = STAT_TYPE.STAT_HP_CUR, statValue = health })
            .ToArray();
        var actor = ecs.Get<ActorComponent>(creature);

        Broadcast(new MonsterUpdateStatListResponse { Actor = actor, Stats = changes });

        if (after.MaxSuperArmor > 0f)
        {
            Broadcast(new MonsterSuperArmorGageResponse { Actor = actor, Current = after.SuperArmor, Max = after.MaxSuperArmor });
        }
    }

    /// <summary>
    /// A monster with a script thinks. Objects and the rest just stand.
    /// </summary>
    private void GiveMind(Entity creature, int identifier, Vector3 home, MonsterSpawnBox spawn)
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

        body.SetGroup(new AggroGroup(spawn.AggroGroup, spawn.AggroDistance, spawn.AggroMaxCount));
        body.SetSpawnAction(spawn.ChangeSpawnAction is ChangeSpawnActionType.Empty or ChangeSpawnActionType.Zero ? string.Empty : spawn.ChangeSpawnAction.ToString());

        Bodies.Add(creature, body);
        Minds.Add(body);
    }

    /// <summary>
    /// The mind of a monster that is gone waits for the next monster of its script.
    /// </summary>
    private void Retire(MonsterBody body)
    {
        body.Retired();
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
    /// Whether the box has appeared, or has been told it never will.
    /// </summary>
    public bool HasAppeared(int box) => SpawnedBoxes.Contains(box);

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
            .Where(e => ecs.Get<ActorComponent>(e).Type == ActorType.Monster && !IsHidden(e))
            .Where(e => touches(ecs.Get<WorldComponent>(e).Position, ecs.Get<MonsterCombatComponent>(e)))
            .ToArray();
    }

    /// <summary>
    /// A monster that has hidden cannot be hit while it is gone, and one the players are asked to defend cannot be hit by them at all.
    /// </summary>
    private bool IsHidden(Entity creature)
    {
        return Bodies.TryGetValue(creature, out var body) && body.IsHidden || Helpers.Defended.Contains(creature);
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
            .Where(e => ecs.Get<ActorComponent>(e).Type == ActorType.Monster && !IsHidden(e))
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
    /// The townsman of the maze that the actor names, as a client names it in a request: by the number the maze gave it.
    /// </summary>
    public bool TryFindNpc(int actor, out Entity found)
    {
        foreach (var creature in Creatures)
        {
            if (ecs.IsAlive(creature) && ecs.Get<ActorComponent>(creature) is { Type: ActorType.Npc } place && place.Identifier == actor)
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
    /// Somebody has hurt a monster: the monster remembers who did it, and the more damage and the more the skill is made to draw
    /// hate, the more it cares.
    /// </summary>
    public void OnMonsterHit(Entity monster, Entity attacker, int damage, int skill = 0, AiHitKind kind = AiHitKind.Skill)
    {
        if (Bodies.TryGetValue(monster, out var body) && ecs.IsAlive(attacker))
        {
            var hate = (Services?.Aggro ?? SkillAggro.None).Hate(skill, damage, LevelOrder(attacker));

            body.Machine.OnDamaged(new AiHit(ecs.Get<ActorComponent>(attacker), hate, damage, kind));
        }
    }

    /// <summary>
    /// Something other than a blow angers a monster at a creature of the maze: its hate for that creature grows, and a monster
    /// with no enemy turns on it.
    /// </summary>
    public void Provoke(Entity monster, Entity target, float hate)
    {
        if (Bodies.TryGetValue(monster, out var body) && ecs.IsAlive(target))
        {
            body.Machine.Provoke(ecs.Get<ActorComponent>(target), hate);
        }
    }

    /// <summary>
    /// How many players of the maze are of a lower level than the one who dealt a blow, those of the same level counted by the
    /// order they came in. The lower the level, the more the blow angers a monster.
    /// </summary>
    private int LevelOrder(Entity attacker)
    {
        var at = Players.IndexOf(attacker);

        if (at < 0 || !ecs.TryGet<LevelComponent>(attacker, out var level))
        {
            return 0;
        }

        var order = 0;

        for (var index = 0; index < Players.Count; index++)
        {
            if (index != at && ecs.IsAlive(Players[index]) && ecs.TryGet<LevelComponent>(Players[index], out var other)
                && (other.Level < level.Level || other.Level == level.Level && index < at))
            {
                order++;
            }
        }

        return order;
    }

    /// <summary>
    /// A buff turns the monster on the actor that gave it, for as long as the buff lasts.
    /// </summary>
    internal void Provoke(Entity monster, int actor)
    {
        if (Bodies.TryGetValue(monster, out var body))
        {
            body.Machine.Taunt(actor);
        }
    }

    /// <summary>
    /// The taunt on a monster is over: its hate decides whom it fights again.
    /// </summary>
    internal void EndTaunt(Entity monster)
    {
        if (Bodies.TryGetValue(monster, out var body))
        {
            body.Machine.ReleaseTaunt();
        }
    }

    /// <summary>
    /// How much a monster with super armor can take before it breaks, as a share of its health. The tables of this client
    /// do not say, so this is a guess.
    /// </summary>
    private const float SuperArmorShare = 0.2f;

    /// <summary>
    /// What a hit does to the monster that takes it, as the clients are told in the hit: the defense type and the gauge of the
    /// armor after the blow, and the throw.
    /// </summary>
    public readonly record struct HitEffect(byte Defense, float SuperArmor, Vector3 ExtraMove, float FlySpeed);

    /// <summary>
    /// A monster takes a blow. Its super armor takes the share of the hit that the skill gives it: while the armor holds the
    /// monster shows no reaction and is not moved, unless the blow is hard; when it breaks the monster reacts like any other
    /// and, if it has a motion for it, stands helpless through the break. The reaction then runs on the body of the monster.
    /// </summary>
    /// <param name="defense">The defense type of the monster, buffs counted.</param>
    /// <param name="damage">What the hit deals.</param>
    /// <param name="armorShare">The share of the damage the skill gives to the armor, in percent.</param>
    /// <param name="survives">The monster lives on.</param>
    public HitEffect ReceiveBlow(Entity monster, in Blow blow, DefenseType defense, int damage, float armorShare, bool survives)
    {
        // The clients count the time of a broken armor themselves and break an armor they hold whole on a hit that says it
        // is empty, so an armor whose time is up is whole for this blow, not from the next step of the maze on.
        RestoreSuperArmor(monster);

        var combat = ecs.Get<MonsterCombatComponent>(monster);
        var broken = combat.SuperArmorBrokenUntil != 0;
        var seconds = 0f;
        var gauge = combat.MaxSuperArmor > 0f ? combat.SuperArmor : 0f;
        _ = Bodies.TryGetValue(monster, out var body);

        if (defense == DefenseType.SuperArmor && combat.MaxSuperArmor > 0f)
        {
            if (broken)
            {
                defense = DefenseType.Normal;
            }
            else
            {
                var taken = SuperArmorRules.Take(combat.SuperArmor, SuperArmorRules.Damage(damage, armorShare), blow.Reaction.Hardness);

                gauge = taken.Gauge;

                if (taken.Broken)
                {
                    defense = DefenseType.Normal;
                    seconds = SuperArmorRules.BreakSeconds(Animation(body, "B_SABreak_Start"), Animation(body, "B_SABreak_End"), combat.SuperArmorRecovery);
                    combat = combat with { SuperArmorBrokenUntil = Environment.TickCount64 + (long)(seconds * 1000f) };
                }

                ecs.Set(monster, combat with { SuperArmor = gauge });
            }
        }

        var rolls = BlowRolls.Roll(Random);
        var outcome = new HitOutcome(Vector3.Zero, Vector3.Zero, 0f, false, MotionClass.None);

        if (body is not null)
        {
            if (seconds > 0f && combat.ArmorBreakMotion)
            {
                body.BreakArmor(seconds);
            }

            outcome = body.ReceiveBlow(blow, defense, survives, rolls);
        }
        else
        {
            // A monster with no mind cannot be moved by the server; the clients are told as if it could.
            var profile = new ReactionProfile(combat.WeightRank, combat.ReactionLimit, combat.StandType, combat.Rank, combat.BodyRadius);

            outcome = new MonsterReaction(profile, Navigation, static _ => 0f, Random).Hit(blow, defense, survives, ecs.Get<WorldComponent>(monster).Position, rolls);
        }

        return new HitEffect((byte)defense, gauge, outcome.ExtraMove, outcome.FlySpeed);
    }

    /// <summary>
    /// A weak blow that slows its targets by chance tells everyone it did, before it tells how hard the blow was.
    /// </summary>
    internal void TellHitSlow(SkillDmgValue[] hits, in AttackTrigger trigger)
    {
        if (trigger.Reaction.SlowTime <= 0f)
        {
            return;
        }

        foreach (var hit in hits)
        {
            var time = HitSlow.TimeToTell(trigger.Reaction, (DefenseType)hit.DefenseType, HitSlow.Roll(Random));

            if (time > 0f)
            {
                Broadcast(new MovementStiffenBtResponse { Actor = hit.UxActorId, Rate = trigger.Reaction.SlowRate, Time = time });
            }
        }
    }

    private float Animation(MonsterBody? body, string name)
    {
        return body is null ? 0f : Services?.Actions.GetAnimationLength(body.Behavior.ActionFile, name) ?? 0f;
    }

    /// <summary>
    /// Whether a player stands within a distance of a place, bodies counted: a monster that slides there is stopped.
    /// </summary>
    internal bool IsPlayerNear(Vector3 place, float distance)
    {
        foreach (var player in Players)
        {
            if (ecs.IsAlive(player) && Vitals.IsAlive(player) && AiMath.Distance2D(Vitals.PositionOf(player), place) <= distance + PlayerVitals.BodyRadius)
            {
                return true;
            }
        }

        return false;
    }

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
    /// Armor that has had time to recover is whole again. Says whether it came back now.
    /// </summary>
    internal bool RestoreSuperArmor(Entity monster)
    {
        var combat = ecs.Get<MonsterCombatComponent>(monster);

        if (combat.SuperArmorBrokenUntil == 0 || combat.SuperArmorBrokenUntil > Environment.TickCount64)
        {
            return false;
        }

        ecs.Set(monster, combat with { SuperArmor = combat.MaxSuperArmor, SuperArmorBrokenUntil = 0 });
        Broadcast(new MonsterSuperArmorGageResponse { Actor = ecs.Get<ActorComponent>(monster), Current = combat.MaxSuperArmor, Max = combat.MaxSuperArmor });

        // The mind may have a skill it casts when its armor is whole again.
        if (Bodies.TryGetValue(monster, out var body))
        {
            body.OnSuperArmorRestored();
        }

        return true;
    }

    internal void SetInvincible(Entity monster, bool invincible)
    {
        if (invincible)
        {
            ForceDefense(monster, DefenseType.Invincible);
        }
        else
        {
            RestoreDefense(monster);
        }
    }

    /// <summary>
    /// A monster takes another defense until <see cref="RestoreDefense"/> gives it its own back.
    /// </summary>
    internal void ForceDefense(Entity monster, DefenseType defense)
    {
        if (!ecs.IsAlive(monster))
        {
            return;
        }

        var combat = ecs.Get<MonsterCombatComponent>(monster);

        Defenses.TryAdd(monster, combat.Defense);
        ecs.Set(monster, combat with { Defense = defense });
    }

    internal void RestoreDefense(Entity monster)
    {
        if (ecs.IsAlive(monster) && Defenses.Remove(monster, out var defense))
        {
            ecs.Set(monster, ecs.Get<MonsterCombatComponent>(monster) with { Defense = defense });
        }
    }

    /// <summary>
    /// A monster of a group fights somebody: the monsters of its group within the reach of the spawn box that have no enemy
    /// turn on that one, as many as the box allows. The monster that called counts among them.
    /// </summary>
    internal void AlertGroup(MonsterBody caller, int target)
    {
        var group = caller.Group;

        if (group.Id == 0)
        {
            return;
        }

        var origin = caller.Position;
        var called = 0;

        foreach (var body in Bodies.Values)
        {
            if (body.Group.Id != group.Id || !body.IsRevealed || body.IsDead || Vector3.Distance(body.Position, origin) >= group.Distance)
            {
                continue;
            }

            if (body.Machine.TargetId == AiMachine.NoTarget)
            {
                body.Machine.Alert(target);
            }

            if (++called >= group.MaxCount)
            {
                break;
            }
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

    /// <summary>
    /// A fallen player gets up where they fell, with all their life and a few seconds in which nothing can hurt them. Whether
    /// they may is for the caller to decide (<c>MazeRevival</c>); a player who has not fallen is left as they are.
    /// </summary>
    public void Revive(Entity player, ReviveType type, int owner = 0)
    {
        if (!ecs.IsAlive(player) || !ecs.Has<ServerSessionComponent>(player) || Vitals.IsAlive(player))
        {
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
            Owner = owner,
            Type = (byte)type,
            Position = place.Position,
            Rotation = place.Rotation
        });
    }

    /// <summary>
    /// Brings a player who stands down at once, as a blow that took the last of their life would (the test command of the
    /// chat). Somebody who has fallen already is left as they are.
    /// </summary>
    public void Fell(Entity player)
    {
        if (!ecs.IsAlive(player) || !Vitals.IsAlive(player))
        {
            return;
        }

        Vitals.Hurt(player, int.MaxValue);
        Announce(player);
    }

    /// <summary>
    /// A player has fallen: the maze is told, their buffs go and their gear wears.
    /// </summary>
    private void Announce(Entity victim)
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
    /// Tells every player of the maze, written once for all of them, without boxing a message that writes itself over spans.
    /// </summary>
    public void Broadcast<T>(in T message) where T : struct, IResponseHotspotMessage, IWireWritable
    {
        if (Players.Count == 0)
        {
            return;
        }

        using var frame = Frame.Encode(in message);

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

    internal Vector3 PositionOf(Entity player) => Vitals.PositionOf(player);

    /// <summary>
    /// The server knows the player is in the air for at least this long.
    /// </summary>
    private void Posture(Entity player, long milliseconds)
    {
        var until = new PlayerPostureComponent(Environment.TickCount64 + milliseconds);

        if (ecs.Has<PlayerPostureComponent>(player))
        {
            ecs.Set(player, until);
        }
        else
        {
            ecs.Add(player, until);
        }
    }

    /// <summary>
    /// What the animation of its skill makes of the defense of a player at this moment; the maze, which plays the skills, says.
    /// </summary>
    public Func<Entity, DefenseType>? PlayerFrames { get; set; }

    /// <summary>
    /// How a player takes a hit now: by the animation of its skill, and a buff that makes it stronger.
    /// </summary>
    internal DefenseType DefenseOfPlayer(Entity player)
    {
        var frames = PlayerFrames?.Invoke(player) ?? DefenseType.Normal;

        return Buffs?.DefenseOf(player, frames) ?? frames;
    }

    /// <summary>
    /// How a monster takes a hit now: the defense of its table, unless the animation of the skill it plays says another.
    /// </summary>
    internal DefenseType FrameDefenseOf(Entity monster, DefenseType own)
    {
        return Bodies.TryGetValue(monster, out var body) ? body.FrameDefense(own, ecs.Get<MonsterCombatComponent>(monster).SuperArmorBrokenUntil != 0) : own;
    }

    internal MonsterBody? BodyOf(Entity monster) => Bodies.GetValueOrDefault(monster);

    /// <summary>
    /// What a player's hit needs to know of the monster it hits: its kind, and whether it is in the air or on the ground.
    /// </summary>
    internal DamageContext ContextOf(Entity monster)
    {
        var combat = ecs.Get<MonsterCombatComponent>(monster);
        var body = BodyOf(monster);

        return DamageContext.PlayerHits(new Opponent(combat.Rank, combat.Faction, combat.StatusType), body?.IsAirborne ?? false, body?.IsLying ?? false);
    }

    /// <summary>
    /// Whether a monster of a table lives in the maze.
    /// </summary>
    internal bool IsMonsterAlive(int table)
    {
        return Creatures.Any(e => ecs.IsAlive(e) && ecs.Get<ActorComponent>(e).Type == ActorType.Monster && ecs.Get<CreatureComponent>(e) is { Health: > 0 } creature && creature.Identifier == table);
    }

    /// <summary>
    /// Nothing hurts the player now: it has just come back, or a buff makes it invincible.
    /// </summary>
    internal bool IsShielded(Entity player)
    {
        return Vitals.IsProtected(player) || DefenseOfPlayer(player) == DefenseType.Invincible;
    }

    /// <summary>
    /// A monster may grab a player that lives and is not held by another.
    /// </summary>
    internal bool CanBeGrabbed(Entity player) => Vitals.IsAlive(player) && !Grabbed.Contains(player);

    internal void Grab(Entity player) => Grabbed.Add(player);

    internal void LetGo(Entity player) => Grabbed.Remove(player);

    /// <summary>
    /// The server puts a player at a place: it flew there held by a monster.
    /// </summary>
    internal void Place(Entity player, Vector3 position)
    {
        ecs.Set(player, ecs.Get<WorldComponent>(player) with { Position = position });
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
        if (trigger.ForAllies || ((trigger.Buff > 0 || trigger.Aura > 0) && trigger.DamageRate <= 0f))
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
        var origin = ecs.Get<WorldComponent>(attacker.Entity);
        var weight = ecs.Get<MonsterCombatComponent>(attacker.Entity).WeightRank;
        var kind = DamageContext.MonsterHits(new Opponent(ecs.Get<MonsterCombatComponent>(attacker.Entity).Rank, ecs.Get<MonsterCombatComponent>(attacker.Entity).Faction, ecs.Get<MonsterCombatComponent>(attacker.Entity).StatusType));
        var blow = new Blow(trigger.Reaction, (ReactionKind)(byte)Math.Clamp(trigger.ReactionType, 0, byte.MaxValue), trigger.DamageRate, trigger.Angle, origin.Position, origin.Position, origin.Rotation, true, weight, IsMelee: trigger.HitsArea);

        foreach (var victim in victims)
        {
            if (!Vitals.IsAlive(victim))
            {
                continue;
            }

            var defense = DefenseOfPlayer(victim);
            var shielded = Vitals.IsProtected(victim) || defense == DefenseType.Invincible;
            var result = shielded
                ? new DamageResult(0, HitResults.None)
                : DamageCalculator.Calculate(stats, Vitals.StatsOf(victim), skill, defense, trigger.DamageRate, Random, context: kind);

            if (Buffs is not null && !shielded)
            {
                result = result with { Damage = Buffs.Adjust(attacker.Entity, victim, result.Damage) };
            }

            var left = Vitals.Hurt(victim, result.Damage);

            Buffs?.AfterHit(attacker.Entity, victim, hits.Count, result.Damage, default);

            if (Buffs is not null && !shielded)
            {
                Buffs.OnCondition(victim, result.Flags.HasFlag(HitResults.Critical) ? MazeBuffs.OnCriticallyDamaged : result.Flags.HasFlag(HitResults.Miss) ? MazeBuffs.OnDamagedMissed : MazeBuffs.OnDamaged);
            }

            var shownDefense = shielded ? DefenseType.Invincible : defense;
            var (extraMove, flySpeed) = shielded
                ? (Vector3.Zero, 0f)
                : PlayerThrow.Told(blow, new Struck(Vitals.PositionOf(victim), PlayerVitals.WeightRank, ReactionLimit.None, 0, shownDefense, false), BlowRolls.Roll(Random), Navigation, PlayerVitals.BodyRadius);

            // A player thrown into the air cannot start an ordinary skill until it lands.
            if (!shielded && extraMove.Z > 0f && MonsterReaction.IsLaunch((ReactionKind)(byte)Math.Clamp(trigger.ReactionType, 0, byte.MaxValue)) && left > 0)
            {
                Posture(victim, SkillStates.FlightMilliseconds(extraMove.Z));
            }

            hits.Add(default(SkillDmgValue) with
            {
                UxActorId = ecs.Get<ActorComponent>(victim),
                ReactionType = shielded ? (byte)0 : (byte)trigger.ReactionType,
                DamageFlags = (byte)result.Flags,
                Damage = result.Damage,
                Health = left,
                ExtraMove = extraMove,
                FlySpeed = flySpeed,
                DefenseType = (byte)shownDefense
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
        var told = hits.ToArray();

        TellHitSlow(told, trigger);

        Broadcast(new SkillActionBroadcastResponse
        {
            SkillDmgValueList = told,
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
            Announce(victim);
        }

        Buffs?.GiveFromHit(attacker.Entity, trigger, victims);
    }
}
