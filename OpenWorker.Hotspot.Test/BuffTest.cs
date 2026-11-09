using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.Numerics;
using Arch.Core;
using Lua;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Batch.Actions;
using OpenWorker.Gameplay.Ai;
using OpenWorker.Lua.Ai;
using OpenWorker.Lua.Managers;
using OpenWorker.Navigation;
using OpenWorker.Res;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Buffs;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Lua;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// Buffs as the original server keeps them: a hit gives the buff of its reaction, a buff changes the stats it names until it
/// ends, a second one of a group stacks by the rule of the group, what hurts ticks on the clock and stops at its end or at
/// death, a script gives a buff to a player.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class BuffTest
{
    private const int Sector = 1;

    private const ushort Stun = 900;
    private const ushort Might = 901;
    private const ushort Burn = 902;
    private const ushort Rage = 910;
    private const ushort Fury = 911;
    private const ushort Mend = 10000;

    private const byte FlatAttack = 33;
    private const byte FlatHealth = 13;
    private const byte HealthShare = 14;

    private static BuffRow Row(ushort id, BuffKind kind, BuffEffect effect, int duration, ushort? group = null, int tick = 0, byte overlapType = 0,
        byte overlapCount = 1, byte grade = 1, ushort change = 0, byte stat = 0, int value = 0) =>
        TestRows.Build<BuffRow>(
            ("Id", id), ("AllowedPlaces", 10), ("Kind", (byte)kind), ("Effect", (ushort)effect), ("Group", group ?? id), ("SuccessRate", 10000),
            ("Grade", grade), ("DurationMilliseconds", duration), ("TickMilliseconds", tick), ("OverlapType", overlapType), ("OverlapCount", overlapCount),
            ("ChangeBuff", change), ("Stat1", stat), ("Value1", value));

    private static readonly ReadOnlyCollection<BuffRow> Buffs = new(
    [
        Row(Stun, BuffKind.Debuff, BuffEffect.Stun, 3000),
        Row(Might, BuffKind.Buff, BuffEffect.Stats, 1000, stat: FlatAttack, value: 100),
        Row(Burn, BuffKind.Debuff, BuffEffect.Burn, 3500, tick: 1000, stat: FlatHealth, value: 10),
        Row(Rage, BuffKind.Buff, BuffEffect.Stats, 5000, overlapType: 1, overlapCount: 3, grade: 2, change: Fury),
        Row(Fury, BuffKind.Buff, BuffEffect.Stats, 10000),
        // The tutorial's own: a share of the life back every half second, for as long as it is in the maze.
        Row(Mend, BuffKind.Buff, BuffEffect.Stats, 0, tick: 500, stat: HealthShare, value: 5)
    ]);

    private static Entity Player(World world, RecordingConnection connection, int health = 100)
    {
        return world.Create(
            new ActorComponent(100, ActorType.User),
            new ServerSessionComponent(connection),
            new WorldComponent { Location = 0, Position = Vector3.Zero, Rotation = 0f, Jump = 0 },
            new LevelComponent(10, 0),
            new PersonInfoComponent { Name = "Haru", Hero = Hero.Haru },
            new AbilityComponent
            {
                Health = new AbilityComponentEntry { Current = health, Max = 100 },
                SoulGain = default,
                SoulVapor = default,
                Stamina = default,
                SuperArmor = default,
                Speed = new SpeedComponentEntry { Move = 1f, Attack = 1f }
            });
    }

    private static LuaMaze Maze(World world, Entity player) =>
        TestMazes.Create(world, player, map: PartyMazeTest.MapWithMonster(1000), buffs: new BuffManager(Buffs));

    private static PlayerFrame[] Received(RecordingConnection connection, SkillOpcode command) =>
        connection.Frames.Select(PlayerFrame.Open).Where(e => e.Is(GroupOpcode.Skill, (byte)command)).ToArray();

    private static ushort BuffOf(PlayerFrame frame) => BinaryPrimitives.ReadUInt16LittleEndian(frame.Body.AsSpan(4));

    private static CombatStats Stats(World world, Entity player) =>
        new CombatTables(TestMazes.None<MonsterRow>(), TestMazes.None<StatusRow>(), TestMazes.None<SkillRow>(), TestMazes.None<CharacterInfoRow>()).GetPlayer(world, player);

    /// <summary>
    /// The buff the reaction of an attack names reaches the monster the attack hit, once, and everyone in the maze sees it.
    /// </summary>
    [TestMethod]
    public async Task AHitWhoseReactionNamesABuffGivesItToTheTargetOnce()
    {
        var world = World.Create();

        try
        {
            var connection = new RecordingConnection();
            var player = Player(world, connection);
            var maze = Maze(world, player);

            maze.SpawnSector(Sector);

            var monster = maze.Creatures.FindMonsters((_, _) => true).Single();
            var actor = (int)world.Get<ActorComponent>(monster);
            var hit = new AttackTrigger(1, 0, 0, 0, 0f, 360f, Vector3.Zero, 0, 0, 100, 100, 0, 1f, 1, Buff: Stun);

            maze.Buffs.GiveFromHit(player, hit, [monster]);

            await MetricAssertions.UntilAsync(() => Received(connection, SkillOpcode.BuffUpdateBt).Length > 0, "The buff is shown.");

            var shown = Received(connection, SkillOpcode.BuffUpdateBt);

            Assert.HasCount(1, shown);
            Assert.AreEqual(actor, BinaryPrimitives.ReadInt32LittleEndian(shown[0].Body));
            Assert.AreEqual(Stun, BuffOf(shown[0]));
            Assert.IsTrue(maze.Buffs.IsHeld(monster), "A stun holds the monster.");
            Assert.IsFalse(maze.Buffs.IsHeld(player));
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// A buff raises the stat it names while it lasts, the player is told, and the stat comes back when it runs out.
    /// </summary>
    [TestMethod]
    public async Task ABuffChangesTheStatItNamesAndTheChangeIsUndoneWhenItEnds()
    {
        var world = World.Create();

        try
        {
            var connection = new RecordingConnection();
            var player = Player(world, connection);
            var maze = Maze(world, player);
            var before = Stats(world, player).Physical.AttackMax;

            Assert.IsTrue(maze.Buffs.Give(player, Might, player));
            Assert.AreEqual(before + 100f, Stats(world, player).Physical.AttackMax);

            maze.Buffs.Step(0.6f);
            Assert.AreEqual(before + 100f, Stats(world, player).Physical.AttackMax, "Not over yet.");

            maze.Buffs.Step(0.6f);
            Assert.AreEqual(before, Stats(world, player).Physical.AttackMax, "Over.");

            var stats = new MessageOpcode(GroupOpcode.Character, (byte)CharacterOpcode.UpdateStatList);

            await MetricAssertions.UntilAsync(() => connection.Frames.Select(PlayerFrame.Open).Count(e => e.Opcode.Value == stats.Value) == 2, "The player is told both changes.");
            await MetricAssertions.UntilAsync(() => Received(connection, SkillOpcode.BuffDeleteBt).Length == 1, "The end is shown.");
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// A group that stacks up to three renews its time with every stack, refuses a lower grade, and turns into its change
    /// buff at the top.
    /// </summary>
    [TestMethod]
    public void ASecondBuffOfAGroupFollowsTheStackingRuleOfTheGroup()
    {
        var catalog = new BuffCatalog(Buffs);
        var book = new BuffBook();
        var carrier = new BuffCarrier(1, 100, 100, false);
        var changes = new List<BuffChange>();
        var random = new Random(1);

        Assert.IsTrue(BuffRules.Give(book, catalog, Rage, 7, carrier, random, changes));
        BuffRules.Step(book, catalog, 2f, carrier, changes);

        Assert.IsTrue(BuffRules.Give(book, catalog, Rage, 7, carrier, random, changes));
        Assert.AreEqual(2, book.Buffs.Single().Count);
        Assert.AreEqual(5f, book.Buffs.Single().Remaining, "A group that starts its time again is renewed.");

        // A buff of the group of a lower grade does not stick.
        Assert.IsTrue(catalog.TryGet(Rage, out var rage));
        Assert.IsFalse(BuffRules.Apply(book, catalog, rage with { Id = 999, Grade = 1 }, 7, carrier, changes));

        changes.Clear();
        BuffRules.Give(book, catalog, Rage, 7, carrier, random, changes);

        Assert.AreEqual(Fury, book.Buffs.Single().Buff, "The top of the stack turns into the change buff.");
        Assert.IsTrue(changes.Any(e => e.Kind == BuffChangeKind.Deleted && e.Buff == Rage));
        Assert.IsTrue(changes.Any(e => e.Kind == BuffChangeKind.Updated && e.Buff == Fury));
    }

    /// <summary>
    /// A burn takes its value at every tick of the clock until it runs out; on a player it stops when the player falls.
    /// </summary>
    [TestMethod]
    public async Task ABuffThatHurtsTicksOnTheClockAndStopsAtItsEndOrAtDeath()
    {
        var world = World.Create();

        try
        {
            var connection = new RecordingConnection();
            var player = Player(world, connection, health: 25);
            var maze = Maze(world, player);

            maze.SpawnSector(Sector);

            var monster = maze.Creatures.FindMonsters((_, _) => true).Single();

            world.Set(monster, world.Get<CreatureComponent>(monster) with { Health = 1000 });

            Assert.IsTrue(maze.Buffs.Give(monster, Burn, player));
            Assert.IsTrue(maze.Buffs.Give(player, Burn, monster));

            for (var step = 0; step < 10; step++)
            {
                maze.Buffs.Step(0.5f);
            }

            Assert.AreEqual(970, world.Get<CreatureComponent>(monster).Health, "Three ticks in three and a half seconds, then nothing.");
            Assert.AreEqual(0, world.Get<AbilityComponent>(player).Health.Current, "The third tick fells the player.");

            var died = new MessageOpcode(GroupOpcode.Character, (byte)CharacterOpcode.Die);

            await MetricAssertions.UntilAsync(() => connection.Frames.Select(PlayerFrame.Open).Any(e => e.Opcode.Value == died.Value), "The player falls.");

            // The ticks are queued behind the fall of the player and reach the connection a moment after it.
            await MetricAssertions.UntilAsync(() => Received(connection, SkillOpcode.BuffDamageBt).Length >= 6, "Every tick of both burns is told.");

            var playerActor = (int)world.Get<ActorComponent>(player);
            var hurt = Received(connection, SkillOpcode.BuffDamageBt).Select(e => BinaryPrimitives.ReadInt32LittleEndian(e.Body)).ToArray();

            Assert.AreEqual(3, hurt.Count(e => e == playerActor), "Death ends the burn of the player.");
            Assert.AreEqual(3, hurt.Count(e => e != playerActor));
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// A monster that thinks stands still while a stun holds it: it neither moves nor attacks the player next to it, and acts
    /// again when the stun is over. It needs the tables, the AI scripts and the action files of the client.
    /// </summary>
    [TestMethod]
    public void AStunnedMonsterDoesNotActUntilTheStunEnds()
    {
        var scripts = Environment.GetEnvironmentVariable("OPENWORKER_TEST_AI");
        var actions = Environment.GetEnvironmentVariable("OPENWORKER_TEST_ACTION");

        if (scripts is null)
        {
            Assert.Inconclusive("OPENWORKER_TEST_AI is not set.");
        }

        if (actions is null)
        {
            Assert.Inconclusive("OPENWORKER_TEST_ACTION is not set.");
        }

        var monsters = TestTables.Load<MonsterRow>("tb_Monster");
        var skills = TestTables.Load<SkillRow>("tb_Skill");
        var combat = new CombatTables(monsters, TestTables.Load<StatusRow>("tb_Status"), skills, TestTables.Load<CharacterInfoRow>("tb_Character_Info"));
        var behaviors = new MonsterBehaviorTables(monsters, skills);
        var loader = new AiScriptLoader(NullLogger<AiScriptLoader>.Instance, scripts);
        var clock = TestMazes.Clock();
        var provider = new SkillActionProvider(skills, NullLogger<SkillActionProvider>.Instance) { Folder = actions };
        var services = new CreatureServices(loader, behaviors, provider, clock, new NavMeshProvider());

        // A plain monster of a low level whose script makes it go for the player it sees.
        var monster = monsters.First(e => e.Type == 0 && e.Level is > 1 and < 20 && behaviors.Get(e.Id) is { } behavior &&
            !ReferenceEquals(loader.Get(behavior.AiScript), AiProfile.Empty) && File.Exists(Path.Join(actions, behavior.ActionFile + ".adf")));
        var world = World.Create();

        try
        {
            var connection = new RecordingConnection();
            var player = Player(world, connection);
            var batch = TestMazes.Map(PartyMazeTest.MapWithMonster(monster.Id));
            var creatures = new LuaCreatureManager(world, batch, combat, services, NullLogger.Instance);
            var maze = new LuaMaze(LuaState.Create(), world, player, creatures, new BuffManager(Buffs), TestMazes.Quests(world), batch,
                TestMazes.None<InteractionObjectRow>().IndexBy(e => e.Id), provider, combat, default, null!, clock, NullLogger<LuaMaze>.Instance);

            maze.SpawnSector(Sector);

            var body = creatures.FindMonsters((_, _) => true).Single();
            var actor = world.Get<ActorComponent>(body).Identifier;

            // The player stands next to the monster.
            world.Set(player, world.Get<WorldComponent>(player) with { Position = world.Get<WorldComponent>(body).Position + new Vector3(80f, 0f, 0f) });

            Assert.IsTrue(maze.Buffs.Give(body, Stun, player));

            var before = Acts(connection, actor);

            // Two and a half seconds of the three of the stun.
            for (var step = 0; step < 50; step++)
            {
                maze.Advance(TimeSpan.FromMilliseconds(50));
            }

            Assert.AreEqual(before, Acts(connection, actor), "A held monster does nothing.");

            // The stun ends, and the monster has ten seconds to act.
            for (var step = 0; step < 200 && Acts(connection, actor) == before; step++)
            {
                maze.Advance(TimeSpan.FromMilliseconds(50));
                Thread.Sleep(5);
            }

            Assert.IsFalse(maze.Buffs.IsHeld(body));
            Assert.IsGreaterThan(before, Acts(connection, actor), "Free again, the monster acts.");
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// What a monster has done that the clients saw: moves, turns, battle poses and skills.
    /// </summary>
    private static int Acts(RecordingConnection connection, int actor)
    {
        Thread.Sleep(50);

        return connection.Frames.Select(PlayerFrame.Open)
            .Count(e => (e.Opcode.Group == (byte)GroupOpcode.Move || e.Is(GroupOpcode.Skill, (byte)SkillOpcode.ActiveSkillBt)) && e.Actor == actor);
    }

    /// <summary>
    /// The buff a script gives (the tutorial's, here) is shown to the player and gives its life back at its ticks.
    /// </summary>
    [TestMethod]
    public async Task ABuffAScriptGivesReachesThePlayer()
    {
        var world = World.Create();

        try
        {
            var connection = new RecordingConnection();
            var player = Player(world, connection, health: 50);
            var maze = Maze(world, player);

            maze.ApplyBuff(world.Get<ActorComponent>(player), Mend);

            await MetricAssertions.UntilAsync(() => Received(connection, SkillOpcode.BuffUpdateBt).Any(e => BuffOf(e) == Mend), "The player is shown the buff.");

            Assert.AreEqual(55, world.Get<AbilityComponent>(player).Health.Current, "The buff gives five in a hundred of the life when it begins.");

            maze.Buffs.Step(0.6f);

            Assert.AreEqual(60, world.Get<AbilityComponent>(player).Health.Current, "And again at each tick.");
        }

        finally
        {
            World.Destroy(world);
        }
    }
}
