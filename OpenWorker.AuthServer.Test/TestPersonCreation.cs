using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Items.Components;
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

    [TestMethod]
    public void OutfitIsPutIntoTheSlotsOfItsItems()
    {
        using var world = World.Create();

        var costumes = new CostumeFactory(world, Load<CreateClothRow>("tb_Create_Cloth"), Load<ItemRow>("tb_Item"));
        var storage = new StorageFactory(world).Create(StorageGroup.ShapeEquip);

        costumes.Dress(storage, 121);

        var content = world.Get<StorageContentComponent>(storage);

        int Item(int slot) => world.Get<StorageItemComponent>(content[slot]).Identifier;

        Assert.AreEqual(211020701, Item(6));
        Assert.AreEqual(211021101, Item(8));
        Assert.AreEqual(211021201, Item(9));
        Assert.AreEqual(211020801, Item(11));
        Assert.AreEqual(4, content.SlotList.Count(e => e != Entity.Null));
    }

    [TestMethod]
    [DataRow(10011, DisplayName = "An outfit without a table row")]
    [DataRow(421, DisplayName = "An outfit made of items that cannot be worn")]
    public void OutfitWithoutWearableItemsLeavesTheCharacterWithoutClothes(int outfit)
    {
        using var world = World.Create();

        var costumes = new CostumeFactory(world, Load<CreateClothRow>("tb_Create_Cloth"), Load<ItemRow>("tb_Item"));
        var storage = new StorageFactory(world).Create(StorageGroup.ShapeEquip);

        costumes.Dress(storage, outfit);

        Assert.IsTrue(world.Get<StorageContentComponent>(storage).SlotList.All(e => e == Entity.Null));
    }
}
