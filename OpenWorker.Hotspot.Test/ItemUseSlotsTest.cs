using Arch.Core;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Channel;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Requests;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Res.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The places of the equipment an item opens and what a package gives. Needs the client tables (OPENWORKER_TEST_RES).
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class ItemUseSlotsTest
{
    /// <summary>
    /// The requests of the bag as a server answers them, with nobody around to see a change of gear.
    /// </summary>
    private sealed class Items(World world, InventoryService inventory, ItemUseService use) : ItemServiceBase(world, inventory, use)
    {
        protected override void WornChanged(Entity player, ItemMoveBtResponse notice)
        {
        }

        protected override void ItemUsed(Entity player, ItemUseBtResponse notice)
        {
        }
    }

    private sealed class Rig : IDisposable
    {
        public ItemSetupFixture Fixture { get; } = new();

        public ItemUseService Use { get; }

        public Items Handler { get; }

        public Rig()
        {
            var limits = new ItemUseLimits(Fixture.World, new PersonStateStore(null!), new FakeTimeProvider(), Options.Create(new DailyMissionOptions()));

            Use = new ItemUseService(Fixture.World, Fixture.Inventory, Fixture.Catalog, limits, new([]), new([]), new([]));
            Handler = new Items(Fixture.World, Fixture.Inventory, Use);
        }

        public ItemRow First(Func<ItemRow, ItemClassifyRow, bool> match)
        {
            return ItemSetupFixture.Load<ItemRow>("tb_Item").First(e => Fixture.Catalog.Classify(e.Id) is { } classify && match(e, classify));
        }

        public ValueTask MoveAsync(ItemInstance item, StorageGroup storage, short slot)
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            writer.Write((byte)item.Storage);
            writer.Write(item.Item);
            writer.Write(item.Slot);
            writer.Write((byte)storage);
            writer.Write(-1);
            writer.Write(slot);
            writer.Flush();
            stream.Position = 0;

            return Handler.OnHandleAsync(new ServiceHandleContext(Fixture.Player, CancellationToken.None), new ItemMoveRequest(new BinaryReader(stream)));
        }

        public void Dispose() => Fixture.Dispose();
    }

    /// <summary>
    /// A memory place of the equipment takes no item until the item of the place has opened it; then it takes one.
    /// </summary>
    [TestMethod]
    public async Task AMemoryPlaceTakesNothingUntilItsItemOpensIt()
    {
        using var rig = new Rig();

        rig.Fixture.World.Set(rig.Fixture.Player, new LevelComponent(60, 0));

        const short place = (short)AbilityEquipSlot.Memory1;

        var memory = rig.First((e, c) => c is { ItemUseType: ItemUseType.ABILITY, SlotType: 101 } && e is { LevelLimit: <= 60, ClassLimit: 0 or 1 });
        var opener = rig.First((e, c) => c is { ItemUseType: ItemUseType.EQUIP_SLOT_OPEN, SlotType: 101 } && e.LevelLimit <= 60);

        var item = rig.Fixture.Give(memory.Id, 1);
        var bag = rig.Fixture.Inventory.InventoryOf(rig.Fixture.Player)!;

        await rig.MoveAsync(item, StorageGroup.AbilityEquip, place);

        Assert.IsNull(bag.At(StorageGroup.AbilityEquip, place), "A shut place takes nothing");
        Assert.AreEqual(StorageGroup.Common, item.Storage);

        var key = rig.Fixture.Give(opener.Id, 1);

        Assert.IsTrue((await rig.Use.UseAsync(rig.Fixture.Player, key.Storage, key.Slot, key.Serial, CancellationToken.None)).Ok);

        await rig.MoveAsync(item, StorageGroup.AbilityEquip, place);

        Assert.AreSame(item, bag.At(StorageGroup.AbilityEquip, place), "The opened place takes the item");
    }
}
