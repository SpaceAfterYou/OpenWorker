using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.GateServer.Gameplay;
using OpenWorker.Hotspot.Messages.Response.Person.Values;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.AuthServer.Test;

/// <summary>
/// Checks the rules against the tables shipped with the client:
/// set OPENWORKER_TEST_RES to the folder with the extracted *.res files.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class TestPersonCreation
{
    private static string Folder { get; set; } = string.Empty;
    private static PersonCreationCatalog Catalog { get; set; } = null!;

    [ClassInitialize]
    public static void Initialize(TestContext _)
    {
        Folder = Environment.GetEnvironmentVariable("OPENWORKER_TEST_RES") ?? string.Empty;

        if (string.IsNullOrEmpty(Folder))
        {
            Assert.Inconclusive("OPENWORKER_TEST_RES is not set.");
        }

        Catalog = new PersonCreationCatalog(
            Load<CharacterInfoRow>("tb_Character_Info"),
            Load<CustomizeHairRow>("tb_Customize_Hair"),
            Load<CustomizeEyesRow>("tb_Customize_Eyes"),
            Load<CustomizeSkinRow>("tb_Customize_Skin"));
    }

    private static ReadOnlyCollection<T> Load<T>(string table) where T : struct
    {
        using var reader = new BinaryReader(File.OpenRead(Path.Join(Folder, $"{table}.res")));

        var rows = new T[reader.ReadInt32()];

        for (var index = 0; index < rows.Length; index++)
        {
            rows[index] = (T)Activator.CreateInstance(typeof(T), reader)!;
        }

        return new ReadOnlyCollection<T>(rows);
    }

    private static AppearanceValue Look(short hairStyle, short hairColor, short eyeColor, short skinColor)
    {
        return new AppearanceValue(hairStyle, hairColor, eyeColor, skinColor);
    }

    [TestMethod]
    public void UntouchedFieldsAreAccepted()
    {
        Assert.IsTrue(Catalog.IsOffered(Hero.Haru, Look(0, 0, 0, 0)));
    }

    [TestMethod]
    public void EveryOfferedChoiceIsAccepted()
    {
        Assert.IsTrue(Catalog.IsOffered(Hero.Haru, Look(1101, 2101, 5101, 3101)));
        Assert.IsTrue(Catalog.IsOffered(Hero.Haru, Look(1103, 2103, 5103, 3103)));
        Assert.IsTrue(Catalog.IsOffered(Hero.Stella, Look(1501, 2501, 5501, 3501)));
    }

    [TestMethod]
    public void ChoiceOfAnotherHeroIsRefused()
    {
        Assert.IsFalse(Catalog.IsOffered(Hero.Haru, Look(1201, 0, 0, 0)));
        Assert.IsFalse(Catalog.IsOffered(Hero.Haru, Look(0, 2201, 0, 0)));
        Assert.IsFalse(Catalog.IsOffered(Hero.Haru, Look(0, 0, 5201, 0)));
        Assert.IsFalse(Catalog.IsOffered(Hero.Haru, Look(0, 0, 0, 3201)));
    }

    [TestMethod]
    public void ChoiceInTheWrongFieldIsRefused()
    {
        Assert.IsFalse(Catalog.IsOffered(Hero.Haru, Look(5101, 0, 0, 0)));
        Assert.IsFalse(Catalog.IsOffered(Hero.Haru, Look(0, 0, 3101, 0)));
    }

    [TestMethod]
    public void HairNotOfferedOnTheCreationScreenIsRefused()
    {
        Assert.IsFalse(Catalog.IsOffered(Hero.Haru, Look(1104, 0, 0, 0)));
    }

    [TestMethod]
    public void OutfitMustBelongToTheHero()
    {
        Assert.IsTrue(Catalog.TryGetCharacter(Hero.Haru, out var haru));

        Assert.IsTrue(PersonCreationCatalog.IsOffered(haru, 0));
        Assert.IsTrue(PersonCreationCatalog.IsOffered(haru, 121));
        Assert.IsFalse(PersonCreationCatalog.IsOffered(haru, 221));
    }

    private static InventoryFactory CreateInventories()
    {
        var catalog = new ItemCatalog(
            Load<ItemRow>("tb_Item"),
            Load<ItemClassifyRow>("tb_Item_Classify"),
            Load<InvenSlotExtendRow>("tb_Inven_Slot_Extend"),
            Load<BankSlotExtendRow>("tb_Bank_Slot_Extend"));

        return new InventoryFactory(catalog, Load<CreateClothRow>("tb_Create_Cloth"), new LocalItemSerials());
    }

    private static PersonPersistent Person(int outfit)
    {
        return new PersonPersistent
        {
            Id = 1,
            Name = "Test",
            Hero = Hero.Haru,
            Outfit = outfit,
            DefaultHairStyle = 0,
            DefaultHairColor = 0,
            DefaultEyeColor = 0,
            DefaultSkinColor = 0,
            EquippedHairStyle = 0,
            EquippedHairColor = 0,
            EquippedEyeColor = 0,
            EquippedSkinColor = 0,
            Location = 0,
            PositionX = 0,
            PositionY = 0,
            PositionZ = 0,
            RotationX = 0,
            FatiguePointCommon = 0,
            Account = null!,
            Gate = null!
        };
    }

    [TestMethod]
    public void OutfitIsPutIntoTheSlotsOfItsItems()
    {
        var inventory = CreateInventories().CreateNew(Person(121));
        var shape = inventory.Find(StorageGroup.ShapeEquip)!;

        int Item(int slot) => shape.Slots[slot]!.Item;

        Assert.AreEqual(211020701, Item(6));
        Assert.AreEqual(211021101, Item(8));
        Assert.AreEqual(211021201, Item(9));
        Assert.AreEqual(211020801, Item(11));
        Assert.AreEqual(4, shape.Slots.Count(e => e is not null));
    }

    [TestMethod]
    public void CharacterStartsWithTheWeaponOfItsClass()
    {
        var inventory = CreateInventories().CreateNew(Person(121));

        Assert.AreEqual(111000001, inventory.At(StorageGroup.AbilityEquip, (int)AbilityEquipSlot.SoulWeapon)!.Item);
    }

    [TestMethod]
    [DataRow(10011, DisplayName = "An outfit without a table row")]
    [DataRow(0, DisplayName = "No outfit")]
    public void OutfitWithoutWearableItemsLeavesTheCharacterWithoutClothes(int outfit)
    {
        var inventory = CreateInventories().CreateNew(Person(outfit));

        Assert.IsTrue(inventory.Find(StorageGroup.ShapeEquip)!.Slots.All(e => e is null));
    }

    [TestMethod]
    public void OutfitOfOtherHeroesIsWornInTheSlotOfEachItem()
    {
        var shape = CreateInventories().CreateNew(Person(421)).Find(StorageGroup.ShapeEquip)!;

        Assert.IsTrue(shape.Slots.Any(e => e is not null));
        Assert.IsTrue(shape.Slots.OfType<ItemInstance>().All(e => e.Slot >= 0 && shape.Slots[e.Slot] == e));
    }
}
