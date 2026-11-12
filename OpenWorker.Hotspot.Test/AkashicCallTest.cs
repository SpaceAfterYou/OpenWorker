using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Lua.Ai;
using OpenWorker.Lua.Managers;
using OpenWorker.UpdateContent.Res.Rows;
using static OpenWorker.Hotspot.Test.MazeRuleRows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// Calling a card of the akashic records into a fight: the card of a quick slot that is in the book, in a maze that allows
/// cards, once until its cooldowns are over; and the akashic skill that brings the called card in, whose blows are shown as the
/// card's.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class AkashicCallTest
{
    private const int Card = 381000101;
    private const int Passive = 382020101;
    private const int CardSkill = 30001;
    private const int AkashicSkill = 30000;
    private const ushort Maze = 21311;
    private const ushort BarredMaze = 21100;

    private static AkashicCalls Calls(World world) => new(
        world,
        new ReadOnlyCollection<AkashicRecordsRow>([
            TestRows.Build<AkashicRecordsRow>(("Id", Card), ("Position", (short)139), ("Type", (byte)1), ("Buff", CardSkill),
                ("Cooldown", 20000), ("CooldownGroup", Card), ("CooldownGlobal", (short)15000)),
            TestRows.Build<AkashicRecordsRow>(("Id", Passive), ("Position", (short)6), ("Type", (byte)4), ("Buff", 50011),
                ("Cooldown", 10000), ("CooldownGroup", Passive))
        ]),
        new ReadOnlyCollection<SkillRow>([TestRows.Build<SkillRow>(("Id", CardSkill)), TestRows.Build<SkillRow>(("Id", AkashicSkill), ("SkillType", (byte)3))]),
        Rules(Rule(Maze), Rule(BarredMaze, cardsBarred: true)),
        TestMazes.None<CharacterInfoRow>());

    /// <summary>
    /// A player with the active card in the first quick slot and the passive one in the second; the book holds the cards named.
    /// </summary>
    private static Entity Player(World world, RecordingConnection connection, params int[] book)
    {
        var bag = new Inventory(1) { QuickSlotCards = [Card, Passive, 0, 0, 0] };

        bag.AkashicRecords.Records.AddRange(book.Select(card => new AkashicRecord { Card = card, Position = card == Card ? 139 : 6 }));

        var life = new AbilityComponentEntry { Current = 100, Max = 100 };

        return world.Create(
            new ActorComponent(1, ActorType.User),
            new ServerSessionComponent(connection),
            new WorldComponent { Location = 0, Position = Vector3.Zero, Rotation = 0f, Jump = 0 },
            new LevelComponent(20, 0),
            new PersonInfoComponent { Name = "Caller", Hero = Hero.Haru },
            new InventoryComponent(bag),
            new AbilityComponent { Health = life, SoulGain = life, SoulVapor = life, Stamina = life, SuperArmor = life, Speed = new SpeedComponentEntry { Move = 1f, Attack = 1f } });
    }

    /// <summary>
    /// A card that brings a creature in is called with the yaw of the player and starts the cooldown of its group and the wait of
    /// all cards: it is refused with 2 until the longer of the two is over.
    /// </summary>
    [TestMethod]
    public void ACalledCardWaitsForItsCooldown()
    {
        var world = World.Create();

        try
        {
            var calls = Calls(world);
            var player = Player(world, new RecordingConnection(), Card);
            var start = TimeSpan.FromMinutes(1);

            Assert.AreEqual(AkashicRefusal.None, calls.Call(player, 0, Maze, 90f, start, out var card));
            Assert.AreEqual(Card, card.Id);
            Assert.AreEqual(new AkashicCallComponent(Card, 90f), world.Get<AkashicCallComponent>(player));

            Assert.AreEqual(AkashicRefusal.Cooldown, calls.Call(player, 0, Maze, 0f, start + TimeSpan.FromSeconds(16), out _), "The group cools down longer than all cards.");
            Assert.AreEqual(AkashicRefusal.None, calls.Call(player, 0, Maze, 0f, start + TimeSpan.FromSeconds(21), out _));
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// Only the card of a quick slot that is in the book can be called, in a maze that allows cards: everything else is refused
    /// with 1, and nothing is called.
    /// </summary>
    [TestMethod]
    public void OnlyACardOfTheBookIsCalledWhereCardsAreAllowed()
    {
        var world = World.Create();

        try
        {
            var calls = Calls(world);
            var outside = Player(world, new RecordingConnection());
            var player = Player(world, new RecordingConnection(), Card);

            Assert.AreEqual(AkashicRefusal.NotUsable, calls.Call(outside, 0, Maze, 0f, TimeSpan.Zero, out _), "The card is not in the book.");
            Assert.AreEqual(AkashicRefusal.NotUsable, calls.Call(player, 2, Maze, 0f, TimeSpan.Zero, out _), "The slot is empty.");
            Assert.AreEqual(AkashicRefusal.NotUsable, calls.Call(player, 5, Maze, 0f, TimeSpan.Zero, out _), "There is no such slot.");
            Assert.AreEqual(AkashicRefusal.NotUsable, calls.Call(player, 0, BarredMaze, 0f, TimeSpan.Zero, out _), "The maze bars cards.");
            Assert.IsFalse(world.Has<AkashicCallComponent>(player));
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// A card that brings nothing in is answered and calls nothing: the akashic skill has no card to bring.
    /// </summary>
    [TestMethod]
    public void APassiveCardIsAnsweredAndCallsNothing()
    {
        var world = World.Create();

        try
        {
            var calls = Calls(world);
            var player = Player(world, new RecordingConnection(), Card, Passive);

            Assert.AreEqual(AkashicRefusal.None, calls.Call(player, 1, Maze, 0f, TimeSpan.Zero, out var card));
            Assert.AreEqual(Passive, card.Id);
            Assert.IsFalse(calls.TryTake(player, out _, out _));
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// With the client's tables and files: the akashic skill of a player who has called a card brings the card in, everybody
    /// sees it come (06/7B, with the offset of the akashic file), and the card's own skill strikes the monster it stands on with
    /// blows shown as the card's (06/13). The call is used up.
    /// </summary>
    [TestMethod]
    public async Task TheAkashicSkillBringsTheCalledCardInAndItStrikesAsItself()
    {
        var world = World.Create();

        try
        {
            var arena = new Arena(world).Open();

            Assert.AreEqual(AkashicRefusal.None, arena.Calls.Call(arena.Player, 0, Maze, 0f, arena.Clock.Time, out _));

            arena.Maze.StartSkill(arena.Player, AkashicSkill, default);
            arena.Run(10f);

            await MetricAssertions.UntilAsync(() => Frames(arena.Connection, SkillOpcode.SkillSummonAkashic).Length == 1, "The player sees the card come.");

            var summon = Frames(arena.Connection, SkillOpcode.SkillSummonAkashic).Single().Body;
            var creature = BinaryPrimitives.ReadInt32LittleEndian(summon.AsSpan(4));

            Assert.AreEqual(1, new ActorComponent(BinaryPrimitives.ReadInt32LittleEndian(summon)).Identifier, "The card is the player's.");
            Assert.AreEqual(ActorType.Akashic, new ActorComponent(creature).Type);
            Assert.AreEqual(-350f, BinaryPrimitives.ReadSingleLittleEndian(summon.AsSpan(4 + 4 + 4 + 4 + 4)), "The offset of the akashic file.");

            await MetricAssertions.UntilAsync(() => Frames(arena.Connection, SkillOpcode.ActionSkillBt).Any(e => Holds(e.Body, creature) && Holds(e.Body, CardSkill)),
                "The card strikes with its own skill, as itself.");

            Assert.IsFalse(world.Has<AkashicCallComponent>(arena.Player), "The call is used up.");

            arena.Maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// A player who falls before the card comes brings nothing in, and the call is spent all the same.
    /// </summary>
    [TestMethod]
    public async Task AFallBeforeTheCardComesCallsItOff()
    {
        var world = World.Create();

        try
        {
            var arena = new Arena(world).Open();

            Assert.AreEqual(AkashicRefusal.None, arena.Calls.Call(arena.Player, 0, Maze, 0f, arena.Clock.Time, out _));

            arena.Maze.StartSkill(arena.Player, AkashicSkill, default);

            var ability = world.Get<AbilityComponent>(arena.Player);

            world.Set(arena.Player, ability with { Health = ability.Health with { Current = 0 } });
            arena.Run(10f);

            // What a step sends is in the queue of the connection by then: wait for it to be written before looking.
            await Task.Delay(300);

            Assert.IsEmpty(Frames(arena.Connection, SkillOpcode.SkillSummonAkashic), "No card comes for a fallen player.");
            Assert.IsFalse(world.Has<AkashicCallComponent>(arena.Player));

            arena.Maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// A maze of the client's tables and files with one monster, and a player who stands 350 behind it, facing it along yaw 0,
    /// with the first card of the client's table in its book and first quick slot: the card comes right onto the monster.
    /// </summary>
    private sealed class Arena(World world)
    {
        public FakeTimeProvider Time { get; } = new();
        public RecordingConnection Connection { get; } = new();
        public SimulationClock Clock { get; private set; } = null!;
        public Entity Player { get; private set; }
        public AkashicCalls Calls { get; private set; } = null!;
        public Lua.LuaMaze Maze { get; private set; } = null!;

        public Arena Open()
        {
            var folder = Environment.GetEnvironmentVariable("OPENWORKER_TEST_ACTION");
            var skills = TestTables.Load<SkillRow>("tb_Skill");

            if (string.IsNullOrEmpty(folder))
            {
                Assert.Inconclusive("OPENWORKER_TEST_ACTION is not set.");
            }

            var heroes = TestTables.Load<CharacterInfoRow>("tb_Character_Info");
            var monsters = TestTables.Load<MonsterRow>("tb_Monster");
            var tables = new CombatTables(monsters, TestTables.Load<StatusRow>("tb_Status"), skills, heroes);
            var monster = monsters.First(e => e.Level is > 1 and < 20 && e.Type == 0 && e.Rank <= 6 && tables.GetMonster(e.Id).Stats.MaxHealth >= 100f);
            var actions = new SkillActionProvider(skills, NullLogger<SkillActionProvider>.Instance) { Folder = folder };

            Clock = TestMazes.Clock(Time);
            Player = AkashicCallTest.Player(world, Connection, Card);
            Calls = new AkashicCalls(world, TestTables.Load<AkashicRecordsRow>("tb_Akashic_Records"), skills, Rules(), heroes);

            var services = new CreatureServices(null!, new Gameplay.Ai.MonsterBehaviorTables(TestMazes.None<MonsterRow>(), TestMazes.None<SkillRow>()), actions, Clock,
                new Navigation.NavMeshProvider(), akashic: Calls);
            var batch = TestMazes.Map(PartyMazeTest.MapWithMonster(monster.Id));
            var creatures = new LuaCreatureManager(world, batch, tables, services);

            Maze = TestMazes.Create(world, Player, clock: Clock, batch: batch, creatures: creatures, actions: actions, tables: tables);

            Assert.IsTrue(Maze.TryStart());
            Maze.MarkLoaded(Player);
            Maze.SpawnSector(1);

            var target = world.Get<WorldComponent>(creatures.FindMonsters((_, _) => true).Single()).Position;

            world.Set(Player, world.Get<WorldComponent>(Player) with { Position = target + new Vector3(0f, 350f, 0f) });

            return this;
        }

        public void Run(float seconds)
        {
            for (var tick = 0; tick < seconds * 20; tick++)
            {
                Time.Advance(SimulationClock.Period);
                Clock.Tick();
            }
        }
    }

    private static PlayerFrame[] Frames(RecordingConnection connection, SkillOpcode command) =>
        connection.Frames.Select(PlayerFrame.Open).Where(e => e.Is(GroupOpcode.Skill, (byte)command)).ToArray();

    private static bool Holds(byte[] body, int value)
    {
        Span<byte> bytes = stackalloc byte[4];

        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);

        return body.AsSpan().IndexOf(bytes) >= 0;
    }
}
