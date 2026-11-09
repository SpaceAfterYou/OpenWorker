using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.ItemSetup;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.ItemSetup.Requests;
using OpenWorker.Hotspot.Modules.ItemSetup.Types;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.Res.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The work on items through the handler of the district, against the tables of the client (OPENWORKER_TEST_RES) and, for an
/// upgrade, the server's (OPENWORKER_TEST_SERVER_RES).
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class ItemSetupTest
{
    private static int FirstOfUse(ItemSetupFixture f, ItemUseType use, Func<ItemRow, bool>? also = null)
    {
        return ItemSetupFixture.Load<ItemRow>("tb_Item")
            .Where(e => f.Catalog.Classify(e.Id)?.ItemUseType == use && (also is null || also(e)))
            .Select(e => e.Id)
            .FirstOrDefault();
    }

    [TestMethod]
    public async Task UpgradeWithAFixedRollGoesUpOrBreaksAsTheServerTableSays()
    {
        using var f = new ItemSetupFixture(withServerTables: true);

        var costs = ItemSetupFixture.Load<ReinforceRow>("tb_Reinforce").GroupBy(e => e.Id).ToDictionary(e => e.Key, e => e.First());
        var chances = ItemSetupFixture.Load<ReinforceChanceRow>("tb_Reinforce", ItemSetupFixture.ServerTablesVariable)
            .GroupBy(e => e.Id).ToDictionary(e => e.Key, e => e.First());

        // An item of the bag that both tables know how to upgrade.
        var item = ItemSetupFixture.Load<ItemRow>("tb_Item").First(e =>
            e.Reinforce != 0 && costs.ContainsKey(e.Reinforce) && chances.ContainsKey(e.Reinforce) &&
            f.Catalog.HomeOf(e.Id) == StorageGroup.Common);
        var target = f.Give(item.Id, 1);

        // A level at which the item may go up or break.
        var chance = chances[item.Reinforce];
        var level = Enumerable.Range(ItemUpgradeService.ProtectedFrom, 5)
            .First(e => e < costs[item.Reinforce].MaxLevel && chance.Up[e] > 0 && chance.Break[e] > 0);
        var cost = costs[item.Reinforce].Cost(level)!.Value;

        f.Gold = 100_000_000;
        f.World.Get<CurrencyComponent>(f.Player)[ShopCurrency.Bp] = 1_000_000;
        f.World.Get<CurrencyComponent>(f.Player)[ShopCurrency.Ether] = 1_000_000;

        if (cost.Material != 0)
        {
            f.Give(cost.Material, cost.MaterialCount * 2);
        }

        if (cost.ExtraMaterial != 0)
        {
            f.Give(cost.ExtraMaterial, cost.ExtraMaterialCount * 2);
        }

        var request = new ItemSetupUpgradeRequest { Npc = 1, Target = ItemSetupFixture.SlotOf(target) };

        // The highest roll that still goes up.
        target.Upgrade = (byte)level;
        f.Random.Roll = chance.Up[level];

        await f.Handler.OnHandleAsync(f.Context, request);

        Assert.AreEqual(level + 1, target.Upgrade);
        Assert.AreEqual(0, target.Flag);
        Assert.AreEqual(100_000_000 - cost.Gold, f.Gold, "The gold of the client's table is charged");
        Assert.AreEqual(1_000_000 - cost.BattlePoint, f.World.Get<CurrencyComponent>(f.Player)[ShopCurrency.Bp]);

        // The lowest roll past it breaks the item.
        target.Upgrade = (byte)level;
        f.Random.Roll = chance.Up[level] + 1;

        await f.Handler.OnHandleAsync(f.Context, request);

        Assert.AreEqual(level, target.Upgrade);
        Assert.AreEqual(ItemRestoreService.BrokenFlag, target.Flag);
        Assert.AreEqual(100_000_000 - 2 * cost.Gold, f.Gold);
        Assert.IsTrue(f.Dirty.Sections.HasFlag(PersonSections.Inventory), "The upgraded item is written by the flush");
    }

    [TestMethod]
    public async Task SocketEquipPutsTheGemInAndTheEntryLoadShowsIt()
    {
        using var f = new ItemSetupFixture();

        var sockets = ItemSetupFixture.Load<SocketRow>("tb_Socket").GroupBy(e => e.Id).ToDictionary(e => e.Key, e => e.First());
        var items = ItemSetupFixture.Load<ItemRow>("tb_Item");

        // An item of the bag with a socket to open, and a gem it can take.
        var target = items.First(e => e.Socket != 0 && sockets.TryGetValue(e.Socket, out var row) && row.MaxSockets > 0 &&
                                      f.Catalog.HomeOf(e.Id) == StorageGroup.Common &&
                                      FirstOfUse(f, ItemUseType.ITEM_SOCKET_EQUIP, g => g.Level <= e.Level) != 0);
        var socket = sockets[target.Socket];
        var gemId = FirstOfUse(f, ItemUseType.ITEM_SOCKET_EQUIP, g => g.Level <= target.Level);

        var equipment = f.Give(target.Id, 1);
        var gem = f.Give(gemId, 1);

        if (socket.AddItem != 0)
        {
            f.Give(socket.AddItem, socket.AddCount);
        }

        f.Gold = socket.AddCost + socket.EquipCost;

        await f.Handler.OnHandleAsync(f.Context, new ItemSetupSocketActiveRequest { Target = ItemSetupFixture.SlotOf(equipment), Socket = 0 });

        Assert.AreEqual(1, equipment.SocketActive, "The first socket is open");

        await f.Handler.OnHandleAsync(f.Context, new ItemSetupSocketEquipRequest
        {
            Target = ItemSetupFixture.SlotOf(equipment),
            Gems = [new ItemSetupSocketGem { Gem = ItemSetupFixture.SlotOf(gem), Socket = 0 }]
        });

        Assert.AreEqual(gemId, equipment.SocketAt(0).Gem);
        Assert.IsFalse(f.Bag.Items.Any(e => e.Serial == gem.Serial), "The gem is used up");
        Assert.AreEqual(0, f.Gold);

        // What the entry sends with the bags, and what the next entry loads from the row the flush writes.
        var load = f.Inventory.ListSockets(f.Player).Single();

        Assert.AreEqual(equipment.Serial, load.biEquipSerial.Value);
        Assert.AreEqual(gemId, load.dwSocketID);
        Assert.AreEqual(0, load.bySocketPos);
        Assert.AreEqual(gemId, ItemInstance.From(equipment.ToPersistent(7)).SocketAt(0).Gem);
    }

    [TestMethod]
    public async Task RestoreMendsABrokenItemAndUnsealFreesABoundOneAsTheTableSays()
    {
        using var f = new ItemSetupFixture();

        // An item of the bag that may be restored and unsealed.
        var row = ItemSetupFixture.Load<ItemRow>("tb_Item").First(e =>
            e.RestoreLimit > 0 && e.SealLimit > 0 && f.Catalog.HomeOf(e.Id) == StorageGroup.Common);
        var target = f.Give(row.Id, 1);

        var restoring = f.Give(FirstOfUse(f, ItemUseType.WEAPON_RESTORE), 1);
        var unsealing = f.Give(FirstOfUse(f, ItemUseType.UNSEAL), 1);

        target.Flag = ItemRestoreService.BrokenFlag;

        await f.Handler.OnHandleAsync(f.Context, new ItemSetupRestoreRequest
        {
            Tool = ItemSetupFixture.SlotOf(restoring),
            Target = ItemSetupFixture.SlotOf(target)
        });

        Assert.AreEqual(0, target.Flag);
        Assert.AreEqual(1, target.RestoreCount);
        Assert.IsFalse(f.Bag.Items.Any(e => e.Serial == restoring.Serial), "The restoring item is used up");

        target.BindType = ItemRestoreService.Bound;

        await f.Handler.OnHandleAsync(f.Context, new ItemSetupUnsealRequest
        {
            Tool = ItemSetupFixture.SlotOf(unsealing),
            Target = ItemSetupFixture.SlotOf(target)
        });

        Assert.AreEqual(ItemBindType.None, target.BindType);
        Assert.AreEqual(row.SealLimit == 255 ? 0 : 1, target.SealCount);
        Assert.IsFalse(f.Bag.Items.Any(e => e.Serial == unsealing.Serial), "The unsealing item is used up");
    }
}
