using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.ItemSetup;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Types;
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

    // The classify of the attack broaches; the defence and utility ones follow it.
    private const int AttackBroach = 160201;

    /// <summary>
    /// Clothing of the table with an open and a closed place for a broach that the bag does not hold yet, and its row.
    /// </summary>
    private static (ItemRow Clothing, CostumeSocketRow Row) ClothingWithAClosedPlace(ItemSetupFixture f, CostumeBroachPlaces places)
    {
        var clothing = ItemSetupFixture.Load<ItemRow>("tb_Item").First(e =>
            f.Catalog.HomeOf(e.Id) == StorageGroup.Costume && places.StateOf(e.Id).Contains(CostumeBroachPlaces.Closed) &&
            places.StateOf(e.Id).Contains(CostumeBroachPlaces.Open) && !f.Bag.Items.Any(i => i.Item == e.Id));

        return (clothing, places.RowOf(clothing.Id)!.Value);
    }

    [TestMethod]
    public async Task BroachGoesIntoTheOpenedPlaceOfClothingAndTheEntryLoadShowsIt()
    {
        using var f = new ItemSetupFixture();

        var places = new CostumeBroachPlaces(f.Catalog, ItemSetupFixture.Load<CostumeSocketRow>("tb_Costume_Socket"));
        var (clothing, row) = ClothingWithAClosedPlace(f, places);
        var target = f.Give(clothing.Id, 1);

        // New clothing comes with the places of its row; a closed one opens for the item of the row.
        var place = target.BroachState.IndexOf(CostumeBroachPlaces.Closed, StringComparison.Ordinal);

        Assert.AreEqual(places.StateOf(clothing.Id), target.BroachState);
        f.Give(row.OpenItem, row.OpenCount);

        await f.Handler.OnHandleAsync(f.Context, new ItemSetupBroachActiveRequest { Target = ItemSetupFixture.SlotOf(target), Place = (byte)place });

        Assert.AreEqual(CostumeBroachPlaces.Open, target.BroachState[place]);
        Assert.AreEqual(0, f.Inventory.CountOf(f.Player, row.OpenItem), "The opening item is used up");

        // A broach of the kind the place takes.
        var broachId = ItemSetupFixture.Load<ItemRow>("tb_Item").First(e => e.Classify == AttackBroach + place % 3).Id;
        var broach = f.Give(broachId, 1);

        await f.Handler.OnHandleAsync(f.Context, new ItemSetupBroachEquipRequest
        {
            Target = ItemSetupFixture.SlotOf(target),
            Broach = ItemSetupFixture.SlotOf(broach),
            Place = (byte)place
        });

        Assert.AreEqual(broachId, target.Broaches[place]);
        Assert.AreEqual(ST_ITEM_BROACH.ItemCount - 1, target.Broaches.Count(e => e == ItemBroachService.Empty));
        Assert.IsFalse(f.Bag.Items.Any(e => e.Serial == broach.Serial), "The broach is used up");

        // What the entry sends with the bags, and what the next entry loads from the row the flush writes.
        var load = f.Inventory.ListBroaches(f.Player).Single();
        var saved = ItemInstance.From(target.ToPersistent(7));

        Assert.AreEqual(target.Serial, load.biSerial.Value);
        Assert.AreEqual(broachId, load.dwItemID[place]);
        Assert.AreEqual(broachId, saved.Broaches[place]);
        Assert.AreEqual(target.BroachState, saved.BroachState);
        Assert.IsTrue(f.Dirty.Sections.HasFlag(PersonSections.Inventory), "The clothing is written by the flush");
    }

    [TestMethod]
    public async Task ComposeThatPassesItsRollsGivesTheBroachTheServerTableNames()
    {
        using var f = new ItemSetupFixture(withServerTables: true);

        // Five attack broaches of the lowest rank and the first theme, and the item that composes them.
        var material = ItemSetupFixture.Load<ItemRow>("tb_Item").First(e => e.Classify == AttackBroach && e.Rank == 2 && e.EffectValue == 1).Id;
        var tool = f.Give(FirstOfUse(f, ItemUseType.BROACH_COMPOSE), 1);

        Assert.IsTrue(f.Inventory.TryGive(f.Player, material, ItemBroachService.Materials));

        var materials = f.Bag.Items.Where(e => e.Item == material).ToList();
        var toolCount = tool.Count;

        // The rank roll falls past the first chance of its row (3000 of 10000 for five of the lowest rank) into the second, the
        // kind and theme rolls into the first, and the last roll takes the fourth broach of the group.
        f.Random.Queued.Enqueue(3001);
        f.Random.Queued.Enqueue(1);
        f.Random.Queued.Enqueue(1);
        f.Random.Queued.Enqueue(4);

        var expected = ItemSetupFixture.Load<BroachComposeItemRow>("TB_BroachData", ItemSetupFixture.ServerTablesVariable)
            .Single(e => e.Index == 100_000 * 1 + 10_000 * 1 + 1_000 * 2 + 4).Item;

        await f.Handler.OnHandleAsync(f.Context, new ItemSetupBroachComposeRequest
        {
            Tool = ItemSetupFixture.SlotOf(tool),
            Materials = materials.Select(ItemSetupFixture.SlotOf).ToArray()
        });

        Assert.IsFalse(f.Bag.Items.Any(e => e.Item == material), "The five broaches are used up");
        Assert.AreEqual(toolCount - 1, f.Bag.Items.Where(e => e.Item == tool.Item).Sum(e => e.Count));
        Assert.AreEqual(1, f.Bag.Items.Count(e => e.Item == expected), "The broach of the table is in the bag");
    }

    [TestMethod]
    public async Task RefusedBroachChangesNothing()
    {
        using var f = new ItemSetupFixture();

        var places = new CostumeBroachPlaces(f.Catalog, ItemSetupFixture.Load<CostumeSocketRow>("tb_Costume_Socket"));
        var (clothing, _) = ClothingWithAClosedPlace(f, places);
        var target = f.Give(clothing.Id, 1);
        var state = target.BroachState;
        var closed = state.IndexOf(CostumeBroachPlaces.Closed, StringComparison.Ordinal);

        // A closed place without the item that opens it, and a broach of another kind than its place.
        await f.Handler.OnHandleAsync(f.Context, new ItemSetupBroachActiveRequest { Target = ItemSetupFixture.SlotOf(target), Place = (byte)closed });

        var open = target.BroachState.IndexOf(CostumeBroachPlaces.Open, StringComparison.Ordinal);
        var broach = f.Give(ItemSetupFixture.Load<ItemRow>("tb_Item").First(e => e.Classify == AttackBroach + (open + 1) % 3).Id, 1);

        await f.Handler.OnHandleAsync(f.Context, new ItemSetupBroachEquipRequest
        {
            Target = ItemSetupFixture.SlotOf(target),
            Broach = ItemSetupFixture.SlotOf(broach),
            Place = (byte)open
        });

        Assert.AreEqual(state, target.BroachState);
        Assert.IsEmpty(target.Broaches);
        Assert.IsTrue(f.Bag.Items.Any(e => e.Serial == broach.Serial), "The broach is still in the bag");
    }
}
