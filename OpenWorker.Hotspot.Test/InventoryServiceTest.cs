using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Rewards;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Requests;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The rules of the inventory against the tables of the client: set OPENWORKER_TEST_RES to the folder with the *.res files.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class InventoryServiceTest
{
    private const string TablesVariable = "OPENWORKER_TEST_RES";

    // Cloth, stacks of 50 and a gear the first class can wear.
    private const int Material = 710105001;
    private const int Weapon = 111000001;
    private const int OtherClassWeapon = 112000001;
    private const int SecondWeapon = 521;
    private const int Costume = 251020601;

    private static ReadOnlyCollection<T> Load<T>(string table)
    {
        var folder = Environment.GetEnvironmentVariable(TablesVariable);

        if (string.IsNullOrEmpty(folder))
        {
            Assert.Inconclusive($"{TablesVariable} is not set.");
        }

        using var reader = new BinaryReader(File.OpenRead(Path.Combine(folder, table + ".res")));

        var count = reader.ReadInt32();

        return new ReadOnlyCollection<T>(Enumerable.Range(0, count).Select(_ => (T)Activator.CreateInstance(typeof(T), reader)!).ToArray());
    }

    private sealed class Fixture : IDisposable
    {
        public World World { get; } = World.Create();
        public ItemCatalog Catalog { get; }
        public InventoryService Service { get; }
        public InventoryFactory Factory { get; }
        public Entity Player { get; }
        public Inventory Inventory => Service.InventoryOf(Player)!;

        public Fixture(Hero hero = Hero.Haru, byte level = 1)
        {
            Catalog = new ItemCatalog(
                Load<ItemRow>("tb_Item"),
                Load<ItemClassifyRow>("tb_Item_Classify"),
                Load<InvenSlotExtendRow>("tb_Inven_Slot_Extend"),
                Load<BankSlotExtendRow>("tb_Bank_Slot_Extend"));

            var serials = new LocalItemSerials();

            Service = new InventoryService(World, Catalog, serials);
            Factory = new InventoryFactory(Catalog, Load<CreateClothRow>("tb_Create_Cloth"), serials);

            Player = World.Create(
                Component.GetComponentType(typeof(ActorComponent)),
                Component.GetComponentType(typeof(InventoryComponent)),
                Component.GetComponentType(typeof(LevelComponent)),
                Component.GetComponentType(typeof(PersonInfoComponent)),
                Component.GetComponentType(typeof(CurrencyComponent)));

            var person = new PersonPersistent
            {
                Id = 7,
                Name = "Test",
                Hero = hero,
                DefaultHairStyle = 0, DefaultHairColor = 0, DefaultEyeColor = 0, DefaultSkinColor = 0,
                EquippedHairStyle = 0, EquippedHairColor = 0, EquippedEyeColor = 0, EquippedSkinColor = 0,
                Location = 0, PositionX = 0, PositionY = 0, PositionZ = 0, RotationX = 0, FatiguePointCommon = 0,
                Account = null!, Gate = null!
            };

            World.Set(Player, new InventoryComponent(Factory.CreateNew(person)));
            World.Set(Player, new LevelComponent(level, 0));
            World.Set(Player, new PersonInfoComponent { Name = "Test", Hero = hero });
            World.Set(Player, new CurrencyComponent());
        }

        public void Dispose()
        {
            World.Dispose();
        }
    }

    [TestMethod]
    public void ItemsFillStacksBeforeTakingNewSlots()
    {
        using var f = new Fixture();

        Assert.IsTrue(f.Service.TryGive(f.Player, Material, 120));

        var counts = f.Inventory.Find(StorageGroup.Common)!.Slots.OfType<ItemInstance>().Select(e => (int)e.Count).ToArray();

        CollectionAssert.AreEqual(new[] { 50, 50, 20 }, counts);

        Assert.IsTrue(f.Service.TryGive(f.Player, Material, 40));

        counts = f.Inventory.Find(StorageGroup.Common)!.Slots.OfType<ItemInstance>().Select(e => (int)e.Count).ToArray();

        CollectionAssert.AreEqual(new[] { 50, 50, 50, 10 }, counts);
        Assert.AreEqual(160, f.Service.CountOf(f.Player, Material));
    }

    [TestMethod]
    public void ItemsThatDoNotFitAreNotGivenAtAll()
    {
        using var f = new Fixture();

        var size = f.Inventory.Find(StorageGroup.Common)!.Size;
        var tooMany = (size + 1) * 50;

        Assert.IsFalse(f.Service.TryGive(f.Player, Material, tooMany));
        Assert.AreEqual(0, f.Service.CountOf(f.Player, Material));
        Assert.IsFalse(f.Service.TryGive(f.Player, 123456789, 1), "An item that does not exist is not given");
    }

    [TestMethod]
    public void TakingLeavesTheRestAndFreesEmptySlots()
    {
        using var f = new Fixture();

        f.Service.TryGive(f.Player, Material, 120);

        Assert.IsTrue(f.Service.TryTake(f.Player, Material, 70));
        Assert.AreEqual(50, f.Service.CountOf(f.Player, Material));
        Assert.IsFalse(f.Service.TryTake(f.Player, Material, 51), "More than the person has");
        Assert.AreEqual(50, f.Service.CountOf(f.Player, Material));
    }

    [TestMethod]
    public void NewPersonWearsItsWeaponAndStartsOnLevelOne()
    {
        using var f = new Fixture();

        Assert.AreEqual(Weapon, f.Inventory.At(StorageGroup.AbilityEquip, (int)AbilityEquipSlot.SoulWeapon)!.Item);
        Assert.AreEqual(24, f.Inventory.Find(StorageGroup.Common)!.Size);
        Assert.AreEqual(48, f.Inventory.Find(StorageGroup.Costume)!.Size);
    }

    [TestMethod]
    public void WeaponOfAnotherClassCannotBeWorn()
    {
        using var f = new Fixture();

        f.Service.TryGive(f.Player, OtherClassWeapon, 1);

        var slot = f.Inventory.Find(StorageGroup.Common)!.Slots.OfType<ItemInstance>().Single().Slot;

        f.Service.Move(
            f.Player,
            new MoveItemValue { Storage = StorageGroup.Common, Item = OtherClassWeapon, Index = slot },
            new MoveItemValue { Storage = StorageGroup.AbilityEquip, Item = -1, Index = 0 });

        Assert.AreEqual(Weapon, f.Inventory.At(StorageGroup.AbilityEquip, 0)!.Item);
        Assert.AreEqual(OtherClassWeapon, f.Inventory.At(StorageGroup.Common, slot)!.Item);
    }

    [TestMethod]
    public void WearingSwapsWithWhatIsWornAndKeepsTheSerials()
    {
        using var f = new Fixture();

        var worn = f.Inventory.At(StorageGroup.AbilityEquip, 0)!;

        f.Service.TryGive(f.Player, SecondWeapon, 1);

        var spare = f.Inventory.Find(StorageGroup.Common)!.Slots.OfType<ItemInstance>().Single();
        var spareSerial = spare.Serial;
        var slot = spare.Slot;

        f.Service.Move(
            f.Player,
            new MoveItemValue { Storage = StorageGroup.Common, Item = SecondWeapon, Index = slot },
            new MoveItemValue { Storage = StorageGroup.AbilityEquip, Item = worn.Item, Index = 0 });

        Assert.AreEqual(spareSerial, f.Inventory.At(StorageGroup.AbilityEquip, 0)!.Serial);
        Assert.AreEqual(worn.Serial, f.Inventory.At(StorageGroup.Common, slot)!.Serial);
    }

    [TestMethod]
    public void StacksCombineAndDivide()
    {
        using var f = new Fixture();

        f.Service.TryGive(f.Player, Material, 60);

        var bag = f.Inventory.Find(StorageGroup.Common)!;
        var first = bag.Slots[0]!;
        var second = bag.Slots[1]!;

        Assert.AreEqual(50, first.Count);
        Assert.AreEqual(10, second.Count);

        f.Service.Divide(f.Player, new MoveItemValue { Storage = StorageGroup.Common, Item = first.Item, Index = 0 }, StorageGroup.Common, 5, 20);

        Assert.AreEqual(30, bag.Slots[0]!.Count);
        Assert.AreEqual(20, bag.Slots[5]!.Count);
        Assert.AreNotEqual(bag.Slots[0]!.Serial, bag.Slots[5]!.Serial);

        f.Service.Combine(
            f.Player,
            new MoveItemValue { Storage = StorageGroup.Common, Item = Material, Index = 5 },
            new MoveItemValue { Storage = StorageGroup.Common, Item = Material, Index = 1 },
            20);

        Assert.IsNull(bag.Slots[5], "The emptied stack is gone");
        Assert.AreEqual(30, bag.Slots[1]!.Count);
        Assert.AreEqual(60, f.Service.CountOf(f.Player, Material));

        f.Service.Combine(
            f.Player,
            new MoveItemValue { Storage = StorageGroup.Common, Item = Material, Index = 0 },
            new MoveItemValue { Storage = StorageGroup.Common, Item = Material, Index = 1 },
            30);

        Assert.AreEqual(30, bag.Slots[0]!.Count, "A stack is not combined over its limit");
    }

    [TestMethod]
    public void LineUpPutsTheItemsInOrderAtTheFront()
    {
        using var f = new Fixture();

        f.Service.TryGive(f.Player, Material + 1, 1);
        f.Service.TryGive(f.Player, Material, 1);

        var bag = f.Inventory.Find(StorageGroup.Common)!;
        var item = f.Inventory.Lift(StorageGroup.Common, 1)!;

        f.Inventory.Place(StorageGroup.Common, 9, item);

        f.Service.LineUp(f.Player, StorageGroup.Common);

        Assert.AreEqual(Material, bag.Slots[0]!.Item);
        Assert.AreEqual(Material + 1, bag.Slots[1]!.Item);
        Assert.IsNull(bag.Slots[9]);
    }

    [TestMethod]
    public void ChangesAreHandedOverOnceAndTheNextOnesAreSeparate()
    {
        using var f = new Fixture();

        f.Service.TryGive(f.Player, Material, 10);

        var first = f.Inventory.TakeChanges();

        Assert.IsNotNull(first);
        Assert.AreEqual(2, first.Added.Count, "The weapon and the stack");
        Assert.IsNull(f.Inventory.TakeChanges());

        f.Service.TryGive(f.Player, Material, 5);

        var second = f.Inventory.TakeChanges()!;

        Assert.AreEqual(0, second.Added.Count);
        Assert.AreEqual(1, second.Changed.Count);
        Assert.AreEqual(15, second.Changed[0].Count);

        f.Service.TryTake(f.Player, Material, 15);

        var third = f.Inventory.TakeChanges()!;

        Assert.AreEqual(1, third.Removed.Count);
        Assert.AreEqual(first.Added.Single(e => e.Item == Material).Id, third.Removed[0]);
    }

    [TestMethod]
    public void ItemCreatedAndDestroyedBeforeSavingIsNeverSaved()
    {
        using var f = new Fixture();

        f.Service.TryGive(f.Player, Material, 10);
        f.Service.TryTake(f.Player, Material, 10);

        var changes = f.Inventory.TakeChanges()!;

        Assert.AreEqual(0, changes.Removed.Count);
        Assert.IsFalse(changes.Added.Any(e => e.Item == Material));
    }

    [TestMethod]
    public void ChangesThatCouldNotBeSavedComeBack()
    {
        using var f = new Fixture();

        f.Service.TryGive(f.Player, Material, 10);

        var changes = f.Inventory.TakeChanges()!;

        f.Inventory.Restore(changes);

        var again = f.Inventory.TakeChanges()!;

        Assert.AreEqual(changes.Added.Count, again.Added.Count);
    }

    [TestMethod]
    public void SavedItemsComeBackInTheSameSlotsAfterLoading()
    {
        using var f = new Fixture();

        f.Service.TryGive(f.Player, Material, 120);
        f.Service.TryGive(f.Player, Costume, 1);

        var changes = f.Inventory.TakeChanges()!;

        var person = new PersonPersistent
        {
            Id = 7,
            Name = "Test",
            Hero = Hero.Haru,
            DefaultHairStyle = 0, DefaultHairColor = 0, DefaultEyeColor = 0, DefaultSkinColor = 0,
            EquippedHairStyle = 0, EquippedHairColor = 0, EquippedEyeColor = 0, EquippedSkinColor = 0,
            Location = 0, PositionX = 0, PositionY = 0, PositionZ = 0, RotationX = 0, FatiguePointCommon = 0,
            Account = null!, Gate = null!
        };

        var loaded = f.Factory.Load(person, changes.Added);

        foreach (var original in f.Inventory.Items)
        {
            var copy = loaded.At(original.Storage, original.Slot);

            Assert.IsNotNull(copy);
            Assert.AreEqual(original.Serial, copy.Serial);
            Assert.AreEqual(original.Count, copy.Count);
            Assert.AreEqual(original.Item, copy.Item);
        }
    }

    [TestMethod]
    public void StorageIsExtendedByItsTableAndNeedsItsLevel()
    {
        using var f = new Fixture();

        var step = f.Catalog.Extension(StorageGroup.Common, 1)!.Value;

        Assert.AreEqual(36, step.TotalSlots);

        f.Service.GradeUp(f.Player, StorageGroup.Common);

        Assert.AreEqual(24, f.Inventory.Find(StorageGroup.Common)!.Size, "The level is too low");
    }

    [TestMethod]
    public void GoldCannotGoBelowZero()
    {
        using var f = new Fixture();

        Assert.IsTrue(f.Service.TryAddGold(f.Player, 100));
        Assert.IsFalse(f.Service.TryAddGold(f.Player, -101));
        Assert.AreEqual(100, f.Service.Gold(f.Player));
        Assert.IsTrue(f.Service.TryAddGold(f.Player, -100));
    }

    [TestMethod]
    public void WornGearAddsItsAttackToTheStats()
    {
        using var f = new Fixture();

        var before = EquipmentStats.Calculate(f.Inventory, f.Catalog, Hero.Haru);

        f.Service.TryGive(f.Player, SecondWeapon, 1);

        var slot = f.Inventory.Find(StorageGroup.Common)!.Slots.OfType<ItemInstance>().Single().Slot;

        f.Service.Move(
            f.Player,
            new MoveItemValue { Storage = StorageGroup.Common, Item = SecondWeapon, Index = slot },
            new MoveItemValue { Storage = StorageGroup.AbilityEquip, Item = Weapon, Index = 0 });

        var after = EquipmentStats.Calculate(f.Inventory, f.Catalog, Hero.Haru);

        Assert.AreEqual(1000f, after.PhysicalAttack - before.PhysicalAttack);
        Assert.AreEqual(0f, after.MagicalAttack - before.MagicalAttack);
    }

    [TestMethod]
    public void GearInTheBagAddsNothing()
    {
        using var f = new Fixture();

        var before = EquipmentStats.Calculate(f.Inventory, f.Catalog, Hero.Haru);

        f.Service.TryGive(f.Player, SecondWeapon, 1);

        Assert.AreEqual(before, EquipmentStats.Calculate(f.Inventory, f.Catalog, Hero.Haru));
    }

    [TestMethod]
    public async Task RewardGivesItemsAndMoneyOrNothing()
    {
        using var f = new Fixture();

        var granter = new InventoryRewardGranter(f.Service);
        var reward = new Reward([new RewardItem(Material, 60), new RewardItem(SecondWeapon, 1)], Gold: 500, Ether: 7);

        Assert.IsTrue(await granter.TryGrantAsync(f.Player, reward, RewardSource.DailyMission, CancellationToken.None));
        Assert.AreEqual(60, f.Service.CountOf(f.Player, Material));
        Assert.AreEqual(1, f.Service.CountOf(f.Player, SecondWeapon));
        Assert.AreEqual(500, f.Service.Gold(f.Player));

        var tooMuch = new Reward([new RewardItem(Material, short.MaxValue), new RewardItem(Material, short.MaxValue)], Gold: 5);

        Assert.IsFalse(await granter.TryGrantAsync(f.Player, tooMuch, RewardSource.Attendance, CancellationToken.None));
        Assert.AreEqual(60, f.Service.CountOf(f.Player, Material));
        Assert.AreEqual(500, f.Service.Gold(f.Player), "A reward that does not fit gives no money either");
    }

    [TestMethod]
    public void WornGearStrengthensTheDefenseOfThePlayerToo()
    {
        using var f = new Fixture(level: 20);

        var items = Load<ItemRow>("tb_Item");
        var armor = items.First(e => e.PhysicalDefense > 0 && f.Catalog.EquipPlaceOf(e.Id) is { Storage: StorageGroup.AbilityEquip } && f.Catalog.Limits(e.Id) is (<= 20, 0 or 1));

        var tables = new CombatTables(
            Load<MonsterRow>("tb_Monster"),
            Load<StatusRow>("tb_Status"),
            Load<SkillRow>("tb_Skill"),
            Load<CharacterInfoRow>("tb_Character_Info"),
            items: f.Catalog);

        var bare = tables.GetPlayer(f.World, f.Player);
        var place = f.Catalog.EquipPlaceOf(armor.Id)!.Value.Slots[0];

        f.Inventory.Place(StorageGroup.AbilityEquip, place, f.Service.Create(armor.Id, 1));

        var dressed = tables.GetPlayer(f.World, f.Player);

        Assert.AreEqual(armor.PhysicalDefense, dressed.Physical.Defense - bare.Physical.Defense);
        Assert.AreEqual(20, dressed.Level, "The level is the one the player has reached");
    }
}
