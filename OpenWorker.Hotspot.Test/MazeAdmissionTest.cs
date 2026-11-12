using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Domain.Batch.Enums;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Maze;
using OpenWorker.Gameplay.Modules.Maze.Components;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Progress;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.UpdateContent.Res.Rows;
using static OpenWorker.Hotspot.Test.MazeRuleRows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// What a maze asks of whoever takes its portal beyond the level and the fatigue, in the order of the reference: the soul
/// weapon, the daily limit, the item, for a player alone and for each member of a party, and the item taken as the player comes
/// in. The inventory is the one of the client's item tables (OPENWORKER_TEST_RES).
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MazeAdmissionTest
{
    private const int Maze = 21311;
    private const int OtherMaze = 21411;
    private const int Board = 10033;
    private const int Tutorial = 20001;
    private const int TowerStage = 20325;
    private const int Room = 20011;
    private const int District = 10021;
    private const int Material = 710105001;
    private const int Weapon = 111000001;

    internal static ReadOnlyCollection<T> Load<T>(string table)
    {
        var folder = Environment.GetEnvironmentVariable("OPENWORKER_TEST_RES");

        if (string.IsNullOrEmpty(folder))
        {
            Assert.Inconclusive("OPENWORKER_TEST_RES is not set.");
        }

        using var reader = new BinaryReader(File.OpenRead(Path.Combine(folder, table + ".res")));

        var count = reader.ReadInt32();

        return new ReadOnlyCollection<T>(Enumerable.Range(0, count).Select(_ => (T)Activator.CreateInstance(typeof(T), reader)!).ToArray());
    }

    private sealed class Fixture : IDisposable
    {
        public World World { get; } = World.Create();
        public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 11, 9, 12, 0, 0, TimeSpan.Zero));
        public InventoryService Inventory { get; }
        public InventoryFactory Factory { get; }
        public MazeEntryLimits Limits { get; }
        public MazeAdmission Admission { get; }
        private int _next = 1;

        public Fixture(ReadOnlyCollection<MazeRuleRow> rules, byte level = 6, IMazePortals? portals = null)
        {
            var catalog = new ItemCatalog(
                Load<ItemRow>("tb_Item"),
                Load<ItemClassifyRow>("tb_Item_Classify"),
                Load<InvenSlotExtendRow>("tb_Inven_Slot_Extend"),
                Load<BankSlotExtendRow>("tb_Bank_Slot_Extend"));

            var serials = new LocalItemSerials();
            var mazes = new ReadOnlyCollection<MazeInfoRow>([MazeResultsTest.MazeRow(Maze, level, fatigue: 4), MazeResultsTest.MazeRow(OtherMaze, level, fatigue: 4),
                MazeResultsTest.MazeRow(Tutorial, 0, fatigue: 0, type: 1),
                MazeResultsTest.MazeRow(TowerStage, 0, fatigue: 0, type: 7), MazeResultsTest.MazeRow(Room, 0, fatigue: 0, type: 6)]);

            Inventory = new InventoryService(World, catalog, serials);
            Factory = new InventoryFactory(catalog, Load<CreateClothRow>("tb_Create_Cloth"), serials);

            var fatigue = new FatigueService(World, mazes, Time, Options.Create(new DailyMissionOptions()), NullLogger<FatigueService>.Instance);
            var progress = new SharePointManager(World, new PersonStateStore(null!), SharePointCatalog.Of([]), null!, NullLogger<SharePointManager>.Instance);

            Limits = new MazeEntryLimits(
                World,
                new PersonStateStore(null!),
                rules,
                MazeResultsTest.None<MazeDailyPortalRow>(),
                MazeResultsTest.None<MazeOpenControlRow>(),
                Time,
                Options.Create(new DailyMissionOptions()),
                NullLogger<MazeEntryLimits>.Instance,
                Inventory);

            Admission = new MazeAdmission(World, mazes, fatigue, MazeResultsTest.Scoring(MazeResultsTest.None<MazePlayScoreRow>(), rules), progress, Limits, Inventory, portals: portals);
        }

        /// <summary>
        /// A player with the weapon of its class, the level and plenty of fatigue.
        /// </summary>
        public Entity Player(byte level = 10)
        {
            var id = _next++;

            var player = World.Create(
                Component.GetComponentType(typeof(ActorComponent)),
                Component.GetComponentType(typeof(InventoryComponent)),
                Component.GetComponentType(typeof(LevelComponent)),
                Component.GetComponentType(typeof(PersonInfoComponent)),
                Component.GetComponentType(typeof(CurrencyComponent)),
                Component.GetComponentType(typeof(FatiguePointsComponent)),
                Component.GetComponentType(typeof(MazeEntryComponent)));

            var person = new PersonPersistent
            {
                Id = id, Name = "Test", Hero = Hero.Haru,
                DefaultHairStyle = 0, DefaultHairColor = 0, DefaultEyeColor = 0, DefaultSkinColor = 0,
                EquippedHairStyle = 0, EquippedHairColor = 0, EquippedEyeColor = 0, EquippedSkinColor = 0,
                Location = 0, PositionX = 0, PositionY = 0, PositionZ = 0, RotationX = 0, FatiguePointCommon = 0,
                Account = null!, Gate = null!
            };

            World.Set(player, new ActorComponent(id, ActorType.User));
            World.Set(player, new InventoryComponent(Factory.CreateNew(person)));
            World.Set(player, new LevelComponent(level, 0));
            World.Set(player, new PersonInfoComponent { Name = "Test", Hero = Hero.Haru });
            World.Set(player, new CurrencyComponent());
            World.Set(player, new FatiguePointsComponent { Common = 200, Bonus = 0, RefilledAt = Time.GetUtcNow().UtcDateTime });
            World.Set(player, new MazeEntryComponent { Person = id, Book = new MazeEntryBook { Day = Limits.Today } });

            return player;
        }

        public void DisarmPlayer(Entity player)
        {
            Inventory.InventoryOf(player)!.Find(StorageGroup.AbilityEquip)!.Slots[(int)AbilityEquipSlot.SoulWeapon] = null;
        }

        public void Dispose()
        {
            World.Dispose();
        }
    }

    /// <summary>
    /// The portals of a map, as the test says: whether the player stands at one, and whether the map has one.
    /// </summary>
    private sealed class Portals : IMazePortals
    {
        public bool At { get; set; }

        public bool InMap { get; set; }

        public Dictionary<int, PortalWay> Ways { get; } = [];

        public bool IsAt(Entity player, int portal) => At;

        public bool Exists(Entity player, int portal) => InMap;

        public PortalWay? WayOf(Entity player, int portal) => Ways.TryGetValue(portal, out var way) ? way : null;
    }

    /// <summary>
    /// A portal leads where its map says, from the start point the map says: a client that stands at one portal cannot name
    /// another location or another start point (04/01). The map of the event is left at the place the portal names for the
    /// district it stands in, which the client does not send; a maze is entered at its first start point.
    /// </summary>
    [TestMethod]
    public void APortalLeadsWhereItsMapSaysFromTheStartPointItNames()
    {
        const int toDistrict = 30003, toEvent = 30021, toBoard = 21107, inside = 30101, eventMap = 30021;

        var portals = new Portals();

        portals.Ways[toDistrict] = new PortalWay(JumpType.Field, District, 1002102, 0);
        portals.Ways[toEvent] = new PortalWay(JumpType.District6, eventMap, 3002102, 0);
        portals.Ways[toBoard] = new PortalWay(JumpType.Maze, 0, 0, Board);
        portals.Ways[inside] = new PortalWay(JumpType.JumpPos, 0, 3010102, 0);

        using var f = new Fixture(Rules(Rule(Maze, portal: Board), Rule(OtherMaze, portal: Board + 1)), portals: portals);

        var player = f.Player();

        Assert.IsNull(f.Admission.CheckWay(player, District, toDistrict, 1002102, out var start));
        Assert.AreEqual(1002102, start, "a district is entered at the start point of the portal");
        Assert.AreEqual(MazeAdmission.NotOpened, f.Admission.CheckWay(player, District, toDistrict, 1002103, out _), "another start point");
        Assert.AreEqual(MazeAdmission.NotOpened, f.Admission.CheckWay(player, eventMap, toDistrict, 1002102, out _), "another location");

        Assert.IsNull(f.Admission.CheckWay(player, eventMap, toEvent, 0, out start));
        Assert.AreEqual(3002102, start, "the map of the event is entered at the place of the portal");
        Assert.AreEqual(MazeAdmission.NotOpened, f.Admission.CheckWay(player, eventMap, toEvent, 3002104, out _), "a start point the client does not send");

        Assert.IsNull(f.Admission.CheckWay(player, Maze, toBoard, 0, out start), "a maze the board offers");
        Assert.AreEqual(0, start, "at its first start point");
        Assert.AreEqual(MazeAdmission.NotOpened, f.Admission.CheckWay(player, OtherMaze, toBoard, 0, out _), "a maze of another board");
        Assert.AreEqual(MazeAdmission.NotOpened, f.Admission.CheckWay(player, District, toBoard, 0, out _), "a district at a board");

        Assert.AreEqual(MazeAdmission.NotOpened, f.Admission.CheckWay(player, District, inside, 3010102, out _), "a move within the map");
        Assert.AreEqual(MazeAdmission.NotOpened, f.Admission.CheckWay(player, District, 12345, 0, out _), "a portal the map does not have");

        Assert.IsNull(f.Admission.CheckWay(player, Tutorial, 0, 2000105, out start), "the tutorial from wherever the story puts the player");
        Assert.AreEqual(0, start, "at its first start point");
    }

    /// <summary>
    /// The maze board (04/41) leads into the mazes it offers: those whose row names the board.
    /// </summary>
    [TestMethod]
    public void TheMazeBoardLeadsIntoTheMazesItOffers()
    {
        const int toBoard = 21107, toDistrict = 30003;

        var portals = new Portals();

        portals.Ways[toBoard] = new PortalWay(JumpType.Maze, 0, 0, Board);
        portals.Ways[toDistrict] = new PortalWay(JumpType.Field, District, 1002102, 0);

        using var f = new Fixture(Rules(Rule(Maze, portal: Board), Rule(OtherMaze, portal: Board + 1)), portals: portals);

        var player = f.Player();

        Assert.IsNull(f.Admission.CheckBoard(player, Maze, toBoard));
        Assert.AreEqual(MazeAdmission.NotOpened, f.Admission.CheckBoard(player, OtherMaze, toBoard), "a maze of another board");
        Assert.AreEqual(MazeAdmission.NotOpened, f.Admission.CheckBoard(player, Maze, toDistrict), "a portal without a board");
        Assert.AreEqual(MazeAdmission.NotOpened, f.Admission.CheckBoard(player, Maze, 12345), "a portal the map does not have");
    }

    [TestMethod]
    public void APortalIsTakenFromWhereThePlayerStandsAndAMemberNeedsItInItsMap()
    {
        var portals = new Portals();

        using var f = new Fixture(Rules(Rule(Maze)), portals: portals);

        var player = f.Player();

        // A maze and a district are entered from a portal of the map; the tutorial from wherever the story puts the player.
        Assert.AreEqual(MazeAdmission.NotOpened, f.Admission.Check(player, Maze, 21511), "a maze, away from the portal");
        Assert.AreEqual(MazeAdmission.NotOpened, f.Admission.Check(player, District, 21511), "a district, away from the portal");
        Assert.IsNull(f.Admission.Check(player, Tutorial, 21511), "the tutorial maze");

        portals.At = true;

        Assert.IsNull(f.Admission.Check(player, Maze, 21511));
        Assert.IsNull(f.Admission.Check(player, District, 21511));

        // The master of a party is not asked where it stands; each member needs the portal in its map.
        portals.At = false;

        Assert.AreEqual(MazeAdmission.MemberNoPortal, f.Admission.CheckParty(player, [player], Maze, 21511));

        portals.InMap = true;

        Assert.IsNull(f.Admission.CheckParty(player, [player], Maze, 21511));
    }

    [TestMethod]
    public void AStageOfTheTowerAndARoomAreNotEnteredThroughAPortal()
    {
        using var f = new Fixture(Rules(Rule(Maze)));

        var player = f.Player();

        // Their own requests lead into them: those count the day, keep the order of the stages and find the room.
        Assert.AreEqual(MazeAdmission.Closed, f.Admission.Check(player, TowerStage));
        Assert.AreEqual(MazeAdmission.Closed, f.Admission.Check(player, Room));
    }

    [TestMethod]
    public void APlayerAloneIsRefusedByEachRuleOfTheMazeInTheOrderOfTheReference()
    {
        // The maze may be entered once a day and asks for two of an item.
        using var f = new Fixture(Rules(Rule(Maze, limit: 1, item: Material, itemCount: 2)));

        var player = f.Player();

        f.DisarmPlayer(player);

        var armed = f.Player();

        Assert.AreEqual(MazeAdmission.NoWeapon, f.Admission.Check(player, 21999), "the weapon comes before the row of the maze");
        Assert.AreEqual(MazeAdmission.NoMaze, f.Admission.Check(armed, 21999), "a maze the table has no row for");

        Assert.AreEqual(MazeAdmission.NoItem, f.Admission.Check(armed, Maze, 0, out var item));
        Assert.AreEqual(Material, item, "the answer names the item the maze asks for");

        f.Inventory.TryGive(armed, Material, 2);

        Assert.IsNull(f.Admission.Check(armed, Maze), "the item is there");

        // Entering takes the item and counts the entry; the limit stands before the item and the fatigue.
        Assert.IsTrue(f.Limits.Entered(armed, Maze));
        Assert.AreEqual(0, f.Inventory.CountOf(armed, Material), "the item is taken as the player comes in");
        Assert.AreEqual(MazeEntryLimits.LimitReached, f.Admission.Check(armed, Maze), "once a day");

        // The item is gone from the bag of a player who comes in without it: it is sent away.
        var cheat = f.Player();

        Assert.IsFalse(f.Limits.Entered(cheat, Maze));
    }

    /// <summary>
    /// A player who waits for a maze with the auto matching is asked what the portal asks of a member of a party, wherever the
    /// player stands: the weapon, the level, the item.
    /// </summary>
    [TestMethod]
    public void APlayerWhoWaitsForAMazeIsAskedWhatItsPortalAsks()
    {
        using var f = new Fixture(Rules(Rule(Maze, item: Material, itemCount: 1)));

        var player = f.Player();
        var weak = f.Player(level: 2);

        f.Inventory.TryGive(player, Material, 1);
        f.Inventory.TryGive(weak, Material, 1);

        Assert.IsNull(f.Admission.CheckMatching(player, Maze, out _), "a player who has all the maze asks for may wait");
        Assert.AreEqual(MazeAdmission.LevelTooLow, f.Admission.CheckMatching(weak, Maze, out _), "the level");

        f.Inventory.TryTake(player, Material, 1);

        Assert.AreEqual(MazeAdmission.NoItem, f.Admission.CheckMatching(player, Maze, out var item), "the item");
        Assert.AreEqual(Material, item, "the answer names it");

        f.DisarmPlayer(player);

        Assert.AreEqual(MazeAdmission.NoWeapon, f.Admission.CheckMatching(player, Maze, out _), "the weapon stands first");
    }

    [TestMethod]
    public void EachMemberOfAPartyIsCheckedWithTheCodesOfAMember()
    {
        using var f = new Fixture(Rules(Rule(Maze, limit: 1, item: Material, itemCount: 1)));

        var master = f.Player();
        var member = f.Player();
        var weak = f.Player(level: 2);

        f.Inventory.TryGive(master, Material, 1);
        f.Inventory.TryGive(weak, Material, 1);

        // The member has no item: the answer names it.
        Assert.AreEqual(MazeAdmission.MemberNoItem, f.Admission.CheckParty(master, [master, member], Maze, 0, out var item));
        Assert.AreEqual(Material, item);

        f.Inventory.TryGive(member, Material, 1);

        Assert.IsNull(f.Admission.CheckParty(master, [master, member], Maze), "all of them have what the maze asks for");
        Assert.AreEqual(MazeAdmission.MemberLevelTooLow, f.Admission.CheckParty(master, [master, weak], Maze), "the level of a member");

        f.DisarmPlayer(member);

        Assert.AreEqual(MazeAdmission.MemberNoWeapon, f.Admission.CheckParty(master, [master, member], Maze), "the weapon of a member");

        // The master is held to the limit of the day as each member is, and the limit stands before the item.
        f.Limits.Entered(master, Maze);

        Assert.AreEqual(MazeEntryLimits.LimitReached, f.Admission.CheckParty(master, [master], Maze));
    }
}
