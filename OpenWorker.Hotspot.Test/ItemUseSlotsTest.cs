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

        public Rig(ItemPackageRow[]? packages = null, ItemPackageUnpackRow[]? unpacking = null)
        {
            var limits = new ItemUseLimits(Fixture.World, new PersonStateStore(null!), new FakeTimeProvider(), Options.Create(new DailyMissionOptions()));

            Use = new ItemUseService(Fixture.World, Fixture.Inventory, Fixture.Catalog, limits, new(packages ?? []), new([]), new([]),
                unpacking: new(unpacking ?? []), reinforces: ItemSetupFixture.Load<ReinforceRow>("tb_Reinforce"));
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

        public ValueTask<ItemUseResult> UseAsync(ItemInstance item) =>
            Use.UseAsync(Fixture.Player, item.Storage, item.Slot, item.Serial, CancellationToken.None);

        public void Dispose() => Fixture.Dispose();
    }

    /// <summary>
    /// A row of a table as its file holds it: the integers, then the bytes.
    /// </summary>
    private static T Row<T>(int[] integers, byte[]? bytes = null, int[]? after = null)
    {
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);

        foreach (var value in integers)
        {
            writer.Write(value);
        }

        writer.Write(bytes ?? []);

        foreach (var value in after ?? [])
        {
            writer.Write(value);
        }

        writer.Flush();
        stream.Position = 0;

        return (T)Activator.CreateInstance(typeof(T), new BinaryReader(stream))!;
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

    /// <summary>
    /// A package gives each of its fifteen items with its count; an item the server's row marks as upgraded comes as one item
    /// at the level its count names, and a level the item cannot reach refuses the package.
    /// </summary>
    [TestMethod]
    public async Task APackageGivesItsItemsWithTheirCountsAndAnUpgradedItemAtItsLevel()
    {
        using var probe = new Rig();

        var reinforces = ItemSetupFixture.Load<ReinforceRow>("tb_Reinforce").ToDictionary(e => e.Id);
        var box = probe.First((e, c) => c.ItemUseType == ItemUseType.PACKAGE && e is { LevelLimit: <= 1, ClassLimit: 0 });
        var gear = probe.First((e, c) => probe.Fixture.Catalog.StackLimit(e.Id) == 1 && probe.Fixture.Catalog.HomeOf(e.Id) == StorageGroup.Common &&
                                         reinforces.TryGetValue(e.Reinforce, out var r) && r.MaxLevel >= 5);
        var stack = probe.First((e, c) => probe.Fixture.Catalog.StackLimit(e.Id) >= 10 && probe.Fixture.Catalog.HomeOf(e.Id) == StorageGroup.Common);
        var top = reinforces[gear.Reinforce].MaxLevel;

        int[] Package(int level)
        {
            var row = new int[34];

            row[0] = box.Id;
            row[4] = gear.Id;
            row[4 + 14] = stack.Id;
            row[19] = level;
            row[19 + 14] = 3;

            return row;
        }

        int[] Unpack() => [box.Id, 0, 0, 0, gear.Id, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, stack.Id];

        byte[] kinds = [ItemPackageUnpackRow.Upgraded, .. new byte[14]];

        using (var rig = new Rig([Row<ItemPackageRow>(Package(top + 1))], [Row<ItemPackageUnpackRow>(Unpack(), kinds, new int[15])]))
        {
            var item = rig.Fixture.Give(box.Id, 1);

            Assert.AreEqual(52013, (await rig.UseAsync(item)).Error, "A level past what the item can reach refuses the package");
            Assert.AreEqual(1, item.Count, "The package stays");
        }

        using (var rig = new Rig([Row<ItemPackageRow>(Package(top))], [Row<ItemPackageUnpackRow>(Unpack(), kinds, new int[15])]))
        {
            var item = rig.Fixture.Give(box.Id, 1);

            Assert.IsTrue((await rig.UseAsync(item)).Ok);

            var bag = rig.Fixture.Inventory.InventoryOf(rig.Fixture.Player)!;
            var given = bag.Items.Single(e => e.Item == gear.Id);

            Assert.AreEqual((short)1, given.Count, "An upgraded item comes as one");
            Assert.AreEqual((byte)top, given.Upgrade, "at the level its count names");
            Assert.AreEqual(3L, rig.Fixture.Inventory.CountOf(rig.Fixture.Player, stack.Id), "The last of the fifteen items comes with its count");
        }
    }
}
