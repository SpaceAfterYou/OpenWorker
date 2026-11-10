using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
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
    private const int Material = 710105001;
    private const int Weapon = 111000001;

    private static ReadOnlyCollection<T> Load<T>(string table)
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

        public Fixture(ReadOnlyCollection<MazeRuleRow> rules, byte level = 6)
        {
            var catalog = new ItemCatalog(
                Load<ItemRow>("tb_Item"),
                Load<ItemClassifyRow>("tb_Item_Classify"),
                Load<InvenSlotExtendRow>("tb_Inven_Slot_Extend"),
                Load<BankSlotExtendRow>("tb_Bank_Slot_Extend"));

            var serials = new LocalItemSerials();
            var mazes = new ReadOnlyCollection<MazeInfoRow>([MazeResultsTest.MazeRow(Maze, level, fatigue: 4)]);

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

            Admission = new MazeAdmission(World, mazes, fatigue, MazeResultsTest.Scoring(MazeResultsTest.None<MazePlayScoreRow>(), rules), progress, Limits, Inventory);
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
