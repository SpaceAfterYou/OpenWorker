using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.Text;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Maze;
using OpenWorker.Gameplay.Modules.Maze.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Gameplay.Modules.Tower;
using OpenWorker.Gameplay.Modules.Tower.Components;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.UpdateContent.Res.Rows;
using static OpenWorker.Hotspot.Test.MazeRuleRows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The infinite tower: entering a chapter in the order of the reference, the entries of a day, the reward and the climb of a
/// cleared stage, and what the client is told. The chapters are made here; the inventory is the one of the client's item tables
/// (OPENWORKER_TEST_RES).
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class InfiniteTowerTest
{
    private const int Person = 7;
    private const int Item = 820000005;

    /// <summary>
    /// One friend cleared the first chapter and the second, nobody the rest.
    /// </summary>
    private static readonly int[] FriendsOfTheFirstTwoChapters = [1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

    /// <summary>
    /// A chapter of the tower: five stages from the maze <paramref name="first"/> on, the item <see cref="Item"/> for each
    /// stage and the battle points 10 to 50.
    /// </summary>
    private static InfiniteTowerRow Chapter(short chapter, short first)
    {
        using var stream = new MemoryStream();
        using var w = new BinaryWriter(stream);

        w.Write(chapter);
        w.Write((short)(14000 + chapter));
        w.Write((short)1);
        w.Write(Encoding.Unicode.GetBytes("0"));
        w.Write((short)10021);

        for (var stage = 0; stage < 5; stage++)
        {
            w.Write((short)(first + stage));
        }

        for (var stage = 0; stage < 5; stage++)
        {
            w.Write(Item);
        }

        for (var stage = 0; stage < 5; stage++)
        {
            w.Write((byte)1);
        }

        for (var stage = 0; stage < 5; stage++)
        {
            w.Write(10 * (stage + 1));
        }

        for (var stage = 0; stage < 5; stage++)
        {
            w.Write((short)(100 * chapter + stage + 1));
        }

        w.Flush();
        stream.Position = 0;

        using var reader = new BinaryReader(stream);

        return new InfiniteTowerRow(reader);
    }

    private sealed class Ranking : IRankingStore
    {
        public List<(RankingKind Kind, int Scope, int Person, int Time)> Submitted { get; } = [];

        public Task SubmitAsync(RankingKind kind, int scope, int person, int time, int score, CancellationToken cancellationToken = default)
        {
            Submitted.Add((kind, scope, person, time));

            return Task.CompletedTask;
        }
    }

    private sealed class Fixture : IDisposable
    {
        public World World { get; } = World.Create();

        // A Monday, after the start of the game day (11:00).
        public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 11, 9, 12, 0, 0, TimeSpan.Zero));
        public RecordingConnection Connection { get; } = new();
        public InventoryService Inventory { get; }
        public InfiniteTower Tower { get; }
        public Ranking Ranks { get; } = new();
        public PersonDirty Dirty { get; } = new(Person);
        public Entity Player { get; }

        public Fixture()
        {
            var catalog = new ItemCatalog(
                MazeAdmissionTest.Load<ItemRow>("tb_Item"),
                MazeAdmissionTest.Load<ItemClassifyRow>("tb_Item_Classify"),
                MazeAdmissionTest.Load<InvenSlotExtendRow>("tb_Inven_Slot_Extend"),
                MazeAdmissionTest.Load<BankSlotExtendRow>("tb_Bank_Slot_Extend"));

            var serials = new LocalItemSerials();
            var factory = new InventoryFactory(catalog, MazeAdmissionTest.Load<CreateClothRow>("tb_Create_Cloth"), serials);

            Inventory = new InventoryService(World, catalog, serials);

            // The first stage of the first chapter may be entered twice a day.
            var rules = Rules(Rule(20101, type: 7, limit: 2), Rule(20106, type: 7, limit: 2));

            var days = new MazeEntryLimits(
                World,
                new PersonStateStore(null!),
                rules,
                MazeResultsTest.None<MazeDailyPortalRow>(),
                MazeResultsTest.None<MazeOpenControlRow>(),
                Time,
                Options.Create(new DailyMissionOptions()),
                NullLogger<MazeEntryLimits>.Instance);

            Tower = new InfiniteTower(
                World,
                new PersonStateStore(null!),
                new ReadOnlyCollection<InfiniteTowerRow>([Chapter(1, 20101), Chapter(2, 20106)]),
                rules,
                days,
                NullLogger<InfiniteTower>.Instance,
                Inventory,
                ranking: Ranks);

            Player = World.Create(
                Component.GetComponentType(typeof(ActorComponent)),
                Component.GetComponentType(typeof(InventoryComponent)),
                Component.GetComponentType(typeof(PersonInfoComponent)),
                Component.GetComponentType(typeof(CurrencyComponent)),
                Component.GetComponentType(typeof(InfiniteTowerComponent)),
                Component.GetComponentType(typeof(DirtyComponent)),
                Component.GetComponentType(typeof(ServerSessionComponent)));

            var person = new PersonPersistent
            {
                Id = Person, Name = "Test", Hero = Hero.Haru,
                DefaultHairStyle = 0, DefaultHairColor = 0, DefaultEyeColor = 0, DefaultSkinColor = 0,
                EquippedHairStyle = 0, EquippedHairColor = 0, EquippedEyeColor = 0, EquippedSkinColor = 0,
                Location = 0, PositionX = 0, PositionY = 0, PositionZ = 0, RotationX = 0, FatiguePointCommon = 0,
                Account = null!, Gate = null!
            };

            World.Set(Player, new ActorComponent(Person, ActorType.User));
            World.Set(Player, new InventoryComponent(factory.CreateNew(person)));
            World.Set(Player, new PersonInfoComponent { Name = "Test", Hero = Hero.Haru });
            World.Set(Player, new CurrencyComponent());
            World.Set(Player, new InfiniteTowerComponent { Person = Person, Book = new InfiniteTowerBook { Day = days.Today } });
            World.Set(Player, new DirtyComponent(Dirty));
            World.Set(Player, new ServerSessionComponent(Connection));
        }

        public InfiniteTowerBook Book => World.Get<InfiniteTowerComponent>(Player).Book!;

        /// <summary>
        /// What the client was sent, once the writer has delivered at least <paramref name="count"/> frames.
        /// </summary>
        public PlayerFrame[] Sent(int count)
        {
            var until = DateTime.UtcNow.AddSeconds(5);

            while (Connection.Frames.Length < count && DateTime.UtcNow < until)
            {
                Thread.Sleep(10);
            }

            return Connection.Frames.Select(PlayerFrame.Open).ToArray();
        }

        public void Dispose()
        {
            World.Dispose();
        }
    }

    /// <summary>
    /// The climb as 28/01 tells it: the last cleared chapter, the stage over the whole tower, the time (always 0, as the
    /// reference has it) and the entries of the day.
    /// </summary>
    private static (short Chapter, short Stage, long Time, short Count) LoadInfo(PlayerFrame frame)
    {
        Assert.IsTrue(frame.Is(GroupOpcode.InfiniteTower, (byte)InfiniteTowerOpcode.LoadInfo));

        var body = frame.Body.AsSpan();

        Assert.HasCount(14, frame.Body);

        return (BinaryPrimitives.ReadInt16LittleEndian(body), BinaryPrimitives.ReadInt16LittleEndian(body[2..]),
            BinaryPrimitives.ReadInt64LittleEndian(body[4..]), BinaryPrimitives.ReadInt16LittleEndian(body[12..]));
    }

    [TestMethod]
    public void AChapterIsCheckedInTheOrderOfTheReference()
    {
        using var f = new Fixture();

        TowerRefusal? Check(int chapter, bool inParty = false) => f.Tower.CheckEnter(f.Player, chapter, inParty, out _);

        Assert.AreEqual(InfiniteTower.InvalidData, Check(9)?.Code, "a chapter the table does not have");
        Assert.AreEqual(InfiniteTower.WrongChapter, Check(2)?.Code, "the chapter after the next one");

        f.Book.Entries = 2;
        Assert.AreEqual(InfiniteTower.LimitReached, Check(1)?.Code, "the entries of the day are used up");

        f.Book.Entries = 0;
        Assert.AreEqual(InfiniteTower.InParty, Check(1, inParty: true)?.Code, "the tower is climbed alone");

        f.Inventory.InventoryOf(f.Player)!.Find(StorageGroup.AbilityEquip)!.Slots[(int)AbilityEquipSlot.SoulWeapon] = null;
        Assert.AreEqual(InfiniteTower.WorldEnterRequest, Check(1)?.Request, "no soul weapon is answered on the request of a world entry");
        Assert.AreEqual(InfiniteTower.NoWeapon, Check(1)?.Code);

        using var armed = new Fixture();

        Assert.IsNull(armed.Tower.CheckEnter(armed.Player, 1, false, out var maze));
        Assert.AreEqual(20101, maze, "the first stage of the chapter");
        Assert.AreEqual(InfiniteTower.EnterChapterRequest, Check(9)?.Request, "the other refusals answer the request of the chapter");
    }

    [TestMethod]
    public void TheFirstStageOfAChapterCountsAnEntryOfTheDayAndTellsTheClient()
    {
        using var f = new Fixture();

        f.Tower.Entered(f.Player, 20101);
        f.Tower.Entered(f.Player, 20102);

        Assert.AreEqual(1, f.Book.Entries, "the second stage is the same climb");
        Assert.IsTrue(f.Dirty.Sections.HasFlag(PersonSections.MiscState), "the flush writes the book");
        Assert.AreEqual(((short)0, (short)0, 0L, (short)1), LoadInfo(f.Sent(1).Single()));
    }

    [TestMethod]
    public void AClearedStageGivesItsRewardTellsTheClientRaisesTheClimbAndIsRanked()
    {
        using var f = new Fixture();

        Assert.IsTrue(f.Tower.Cleared(f.Player, 20105, 61));
        Assert.IsFalse(f.Tower.Cleared(f.Player, 21311, 61), "a maze that is no stage");

        Assert.AreEqual(1, f.Inventory.CountOf(f.Player, Item), "the item of the stage");
        Assert.AreEqual(50, f.World.Get<CurrencyComponent>(f.Player).BattlePoint, "the battle points of the stage");
        Assert.AreEqual((1, 5), (f.Book.ClearChapter, f.Book.ClearStage), "the last stage clears the chapter");
        Assert.AreEqual((RankingKind.InfiniteTower, 20105, Person, 61), f.Ranks.Submitted.Single());

        var sent = f.Sent(2);
        var reward = sent.Single(e => e.Is(GroupOpcode.InfiniteTower, (byte)InfiniteTowerOpcode.Reward)).Body;

        // The item and its count, the battle points, no extra item, the time in seconds.
        Assert.HasCount(20, reward);
        Assert.AreEqual(Item, BinaryPrimitives.ReadInt32LittleEndian(reward));
        Assert.AreEqual(1, BinaryPrimitives.ReadInt16LittleEndian(reward.AsSpan(4)));
        Assert.AreEqual(50, BinaryPrimitives.ReadInt32LittleEndian(reward.AsSpan(6)));
        Assert.AreEqual(0, BinaryPrimitives.ReadInt32LittleEndian(reward.AsSpan(10)));
        Assert.AreEqual(61, BinaryPrimitives.ReadInt32LittleEndian(reward.AsSpan(16)));

        Assert.AreEqual(((short)1, (short)5, 0L, (short)0), LoadInfo(sent.Single(e => e.Is(GroupOpcode.InfiniteTower, (byte)InfiniteTowerOpcode.LoadInfo))));

        // The next chapter is open now.
        Assert.IsNull(f.Tower.CheckEnter(f.Player, 2, false, out var maze));
        Assert.AreEqual(20106, maze);
    }

    [TestMethod]
    public void TheRankingCountsTheFriendsWhoClearedEachChapterAndShowsMyTimeAndTheFastestOfEachStage()
    {
        using var f = new Fixture();

        const int Friend = 8;
        const int Other = 9;

        TowerClear Clear(int person, int maze, int time, int order) => new(person, $"P{person}", (byte)Hero.Lily, 30, maze, time, order);

        var info = InfiniteTowerRanking.Build(f.Tower, Person, 1,
        [
            Clear(Person, 20101, 70, 1),
            Clear(Friend, 20101, 61, 2),
            Clear(Other, 20101, 61, 3),
            Clear(Person, 20105, 90, 4),
            Clear(Friend, 20105, 95, 5),
            Clear(Friend, 20110, 99, 6)
        ]);

        CollectionAssert.AreEqual(FriendsOfTheFirstTwoChapters, info.FriendsClearedPerChapter, "a friend's clear of the last stage of a chapter");
        Assert.AreEqual((70, 1, 1, Friend, 61), (info.Stages[0].MyClearTime, info.Stages[0].Chapter, info.Stages[0].Stage, info.Stages[0].Person, info.Stages[0].ClearTime),
            "my time and the fastest; of two equal times the first recorded");
        Assert.AreEqual((0, 0), (info.Stages[1].MyClearTime, info.Stages[1].Stage), "a stage nobody cleared");
        Assert.AreEqual((90, 5, Person), (info.Stages[4].MyClearTime, info.Stages[4].Stage, info.Stages[4].Person), "the person may be the fastest");
        Assert.AreEqual(1, info.Chapter);
    }

    [TestMethod]
    public void ANewGameDayAndATicketOnceTheEntriesAreUsedUpClearTheCount()
    {
        using var f = new Fixture();

        f.Tower.Entered(f.Player, 20101);
        Assert.AreEqual(InfiniteTower.EntriesLeft, f.Tower.UseTicket(f.Player), "an entry of the day is left");

        f.Tower.Entered(f.Player, 20101);
        Assert.IsNull(f.Tower.UseTicket(f.Player));
        Assert.AreEqual(0, f.Book.Entries, "the ticket clears the count");

        f.Tower.Entered(f.Player, 20101);
        f.Time.Advance(TimeSpan.FromDays(1));
        f.Tower.TickAsync(CancellationToken.None).AsTask().Wait();

        Assert.AreEqual(0, f.Book.Entries, "a new game day clears the count");
        Assert.AreEqual(((short)0, (short)0, 0L, (short)0), LoadInfo(f.Sent(5)[^1]), "and the client is told");
    }
}
