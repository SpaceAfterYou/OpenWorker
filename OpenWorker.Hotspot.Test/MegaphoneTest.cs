using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Channel.Community;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Res.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The megaphone against the tables of the client: set OPENWORKER_TEST_RES to the folder with the *.res files.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MegaphoneTest
{
    private const string TablesVariable = "OPENWORKER_TEST_RES";

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

    /// <summary>
    /// The words of a megaphone go out only when the slot it names holds a megaphone, and each use takes one from the bag. An
    /// empty slot and an item of another kind are refused with the client's lines, and nothing is taken.
    /// </summary>
    [TestMethod]
    public async Task TheMegaphoneTakesItsItemAndRefusesWithoutIt()
    {
        var items = Load<ItemRow>("tb_Item");
        var catalog = new ItemCatalog(
            items,
            Load<ItemClassifyRow>("tb_Item_Classify"),
            Load<InvenSlotExtendRow>("tb_Inven_Slot_Extend"),
            Load<BankSlotExtendRow>("tb_Bank_Slot_Extend"));

        var megaphone = items.Select(e => (int)e.Id).First(e => catalog.Classify(e)?.ItemUseType == ItemUseType.MEGAPHONE);

        var serials = new LocalItemSerials();
        var factory = new InventoryFactory(catalog, Load<CreateClothRow>("tb_Create_Cloth"), serials);

        using var world = World.Create();

        var inventory = new InventoryService(world, catalog, serials);
        var source = new InventoryMegaphone(inventory);

        var player = world.Create(
            Component.GetComponentType(typeof(ActorComponent)),
            Component.GetComponentType(typeof(InventoryComponent)),
            Component.GetComponentType(typeof(LevelComponent)),
            Component.GetComponentType(typeof(PersonInfoComponent)),
            Component.GetComponentType(typeof(CurrencyComponent)));

        world.Set(player, new InventoryComponent(factory.CreateNew(new PersonPersistent
        {
            Id = 7,
            Name = "Caller",
            Hero = Hero.Haru,
            DefaultHairStyle = 0, DefaultHairColor = 0, DefaultEyeColor = 0, DefaultSkinColor = 0,
            EquippedHairStyle = 0, EquippedHairColor = 0, EquippedEyeColor = 0, EquippedSkinColor = 0,
            Location = 0, PositionX = 0, PositionY = 0, PositionZ = 0, RotationX = 0, FatiguePointCommon = 0,
            Account = null!, Gate = null!
        })));
        world.Set(player, new LevelComponent(1, 0));
        world.Set(player, new PersonInfoComponent { Name = "Caller", Hero = Hero.Haru });

        Assert.IsTrue(inventory.TryGive(player, megaphone, 2));

        var bag = inventory.InventoryOf(player)!;
        var stack = Enum.GetValues<StorageGroup>()
            .SelectMany(e => bag.Find(e)?.Slots.OfType<ItemInstance>() ?? [])
            .Single(e => e.Item == megaphone);
        var empty = (short)Enumerable.Range(0, bag.Find(stack.Storage)!.Size).First(e => bag.At(stack.Storage, e) is null);

        Assert.AreEqual((ushort)0, await source.TakeAsync(player, stack.Storage, stack.Slot));
        Assert.AreEqual(1, inventory.CountOf(player, megaphone), "One use takes one megaphone.");

        Assert.AreEqual(InventoryMegaphone.NoItem, await source.TakeAsync(player, stack.Storage, empty));
        Assert.AreEqual(InventoryMegaphone.NotAMegaphone, await source.TakeAsync(player, StorageGroup.AbilityEquip, 0), "The weapon is no megaphone.");
        Assert.AreEqual(1, inventory.CountOf(player, megaphone), "A refusal takes nothing.");

        Assert.AreEqual((ushort)0, await source.TakeAsync(player, stack.Storage, stack.Slot));
        Assert.AreEqual(InventoryMegaphone.NoItem, await source.TakeAsync(player, stack.Storage, stack.Slot), "The last one is gone.");
    }
}
