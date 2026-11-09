using OpenWorker.Gameplay.Modules.Npcs;
using System.Collections.ObjectModel;
using OpenWorker.DistrictServer.Services;
using OpenWorker.Gameplay.Modules.ItemSetup;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.ItemSetup.Requests;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.Res.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// Production, the exchange of materials, disassembly and evolution through the handler of the district, against the tables
/// of the client (OPENWORKER_TEST_RES) and, for disassembly, the server's (OPENWORKER_TEST_SERVER_RES).
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class ItemCraftTest
{
    /// <summary>
    /// Every roll gives its lowest value: the first item of a row, the least ether and the least count.
    /// </summary>
    private sealed class LowestRandom : Random
    {
        public override int Next(int minValue, int maxValue) => minValue;
    }

    private static ItemCraftService Handler(ItemSetupFixture f, bool withServerTables = false)
    {
        var answers = new ItemSetupAnswers(f.World);
        var random = new LowestRandom();
        var results = withServerTables
            ? ItemSetupFixture.Load<DisassembleResultRow>("tb_Disassemble", ItemSetupFixture.ServerTablesVariable)
            : new ReadOnlyCollection<DisassembleResultRow>([]);

        return new ItemCraftService(
            new ItemMakeService(f.Inventory, ItemSetupFixture.Load<MakeRow>("tb_Make"), answers),
            new ItemExchangeService(f.Inventory, ItemSetupFixture.Load<ReinforceExchangeRow>("tb_Reinforce_Exchange"), answers),
            new ItemDisassembleService(f.Inventory, results, answers, random),
            new ItemEvolutionService(f.Inventory, ItemSetupFixture.Load<ItemEvolutionRow>("tb_Item_Evolution"), answers, random),
            f.Npcs);
    }

    private static long Balance(ItemSetupFixture f, ShopCurrency currency) => f.World.Get<CurrencyComponent>(f.Player)[currency];

    private static long CountOf(ItemSetupFixture f, int item) => f.Inventory.CountOf(f.Player, item);

    [TestMethod]
    public async Task DisassemblyTakesTheItemAndGivesWhatItsRowNames()
    {
        using var f = new ItemSetupFixture();
        var handler = Handler(f, withServerTables: true);

        var results = ItemSetupFixture.Load<DisassembleResultRow>("tb_Disassemble", ItemSetupFixture.ServerTablesVariable)
            .GroupBy(e => e.Id).ToDictionary(e => e.Key, e => e.First());

        // An item of the bag whose row gives its first item with the lowest roll, and a tool that takes items apart.
        var row = ItemSetupFixture.Load<ItemRow>("tb_Item").First(e =>
            e.Disassemble != 0 && results.TryGetValue(e.Disassemble, out var r) && r.Items[0] != 0 && r.Rates[0] > 0 &&
            r.MinCounts[0] > 0 && f.Catalog.Exists(r.Items[0]) && f.Catalog.HomeOf(e.Id) == StorageGroup.Common);
        var result = results[row.Disassemble];
        var toolId = ItemSetupFixture.Load<ItemRow>("tb_Item").First(e => f.Catalog.Classify(e.Id)?.ItemUseType == ItemUseType.DISASSEMBLE).Id;

        var target = f.Give(row.Id, 1);
        var tool = f.Give(toolId, 1);
        var before = CountOf(f, result.Items[0]);

        await handler.OnHandleAsync(f.Context, new ItemSetupDisassembleRequest
        {
            Tool = ItemSetupFixture.SlotOf(tool),
            Target = ItemSetupFixture.SlotOf(target)
        });

        Assert.IsFalse(f.Bag.Items.Any(e => e.Serial == target.Serial), "The item is taken apart");
        Assert.IsFalse(f.Bag.Items.Any(e => e.Serial == tool.Serial), "The tool is used up");
        Assert.AreEqual(before + result.MinCounts[0], CountOf(f, result.Items[0]));
        Assert.AreEqual(result.MinEther, Balance(f, ShopCurrency.Ether));

        // At an NPC two items at once, without a tool: a stack is named once for each of its items.
        f.Give(row.Id, 2);

        await handler.OnHandleAsync(f.Context, new ItemSetupDisassembleExRequest
        {
            Npc = f.NpcOffering(NpcFunction.None),
            Targets = f.Bag.Items.Where(e => e.Item == row.Id).SelectMany(e => Enumerable.Repeat(ItemSetupFixture.SlotOf(e), e.Count)).ToArray()
        });

        Assert.AreEqual(0, CountOf(f, row.Id));
        Assert.AreEqual(before + 3 * result.MinCounts[0], CountOf(f, result.Items[0]));
        Assert.AreEqual(3L * result.MinEther, Balance(f, ShopCurrency.Ether));
    }

    [TestMethod]
    public async Task ExchangeChargesAndGivesPerItsRow()
    {
        using var f = new ItemSetupFixture();
        var handler = Handler(f);

        var row = ItemSetupFixture.Load<ReinforceExchangeRow>("tb_Reinforce_Exchange").First(e =>
            e.ProductCount > 0 && e.MaterialCount > 0 && f.Catalog.Exists(e.Material) && f.Catalog.Exists(e.Product) &&
            e.Material != e.Product);

        f.Give(row.Material, row.MaterialCount * 3);

        // Two exchanges' worth of the product.
        await handler.OnHandleAsync(f.Context, new ItemSetupExchangeRequest { Npc = f.NpcOffering(NpcFunction.Exchange, also: (NpcFunction)row.NpcFunction), Index = row.Id, Count = (byte)(row.ProductCount * 2) });

        Assert.AreEqual(row.MaterialCount, CountOf(f, row.Material));
        Assert.AreEqual(row.ProductCount * 2, CountOf(f, row.Product));
    }

    [TestMethod]
    public async Task EvolutionTurnsTheItemIntoTheTargetOfItsRow()
    {
        using var f = new ItemSetupFixture();
        var handler = Handler(f);

        var evolutions = ItemSetupFixture.Load<ItemEvolutionRow>("tb_Item_Evolution").GroupBy(e => e.Id).ToDictionary(e => e.Key, e => e.First());
        var row = ItemSetupFixture.Load<ItemRow>("tb_Item").First(e =>
            e.Evolution != 0 && evolutions.TryGetValue(e.Evolution, out var r) && f.Catalog.Exists(r.Result1) &&
            f.Catalog.HomeOf(e.Id) == StorageGroup.Common);
        var evolution = evolutions[row.Evolution];

        var target = f.Give(row.Id, 1);

        target.Upgrade = ItemEvolutionService.MinUpgrade;
        f.Gold = evolution.Gold;
        f.World.Get<CurrencyComponent>(f.Player)[ShopCurrency.Bp] = evolution.BattlePoint;
        f.World.Get<CurrencyComponent>(f.Player)[ShopCurrency.Ether] = evolution.Ether;

        if (evolution.Material != 0)
        {
            f.Give(evolution.Material, evolution.MaterialCount);
        }

        await handler.OnHandleAsync(f.Context, new ItemSetupEvolutionRequest { Npc = f.NpcOffering(NpcFunction.Upgrade), Target = ItemSetupFixture.SlotOf(target) });

        Assert.IsFalse(f.Bag.Items.Any(e => e.Serial == target.Serial), "The item gives way to the new one");

        var evolved = f.Bag.Items.Single(e => e.Item == evolution.Result1);

        Assert.AreEqual(ItemEvolutionService.MinUpgrade, evolved.Upgrade, "The new item keeps the upgrade level");
        Assert.AreEqual(0, f.Gold);
        Assert.AreEqual(0, Balance(f, ShopCurrency.Ether));
        Assert.AreEqual(0, evolution.Material == 0 ? 0 : CountOf(f, evolution.Material));
    }

    [TestMethod]
    public async Task MakeChargesTheMaterialsOfTheRecipeAndGivesTheProduct()
    {
        using var f = new ItemSetupFixture();
        var handler = Handler(f);

        // A recipe of the bag with gold and two different materials.
        var recipe = ItemSetupFixture.Load<MakeRow>("tb_Make").First(e =>
            e.Gold > 0 && e.Material2 != 0 && e.Material1 != e.Material2 && f.Catalog.Exists(e.Product) &&
            f.Catalog.HomeOf(e.Product) == StorageGroup.Common && e.Materials.All(m => m.Item == 0 || f.Catalog.Exists(m.Item)));

        foreach (var (material, count) in recipe.Materials.Where(e => e.Item != 0))
        {
            f.Give(material, count * 2);
        }

        f.Gold = recipe.Gold * 2L;

        await handler.OnHandleAsync(f.Context, new ItemSetupMakeRequest { Npc = f.NpcOffering(NpcFunction.Production, recipe.Group), MakeIndex = recipe.Id, MakeCount = 2 });

        Assert.AreEqual(2, CountOf(f, recipe.Product));
        Assert.AreEqual(0, f.Gold);
        Assert.IsTrue(recipe.Materials.Where(e => e.Item != 0 && e.Item != recipe.Product).All(e => CountOf(f, e.Item) == 0),
            "Every material of the recipe is used up");
    }

    [TestMethod]
    public async Task ARefusalChangesNothing()
    {
        using var f = new ItemSetupFixture();
        var handler = Handler(f);

        var recipe = ItemSetupFixture.Load<MakeRow>("tb_Make").First(e =>
            e.Gold > 0 && e.Material2 != 0 && e.Material1 != e.Material2 && f.Catalog.Exists(e.Product) &&
            f.Catalog.HomeOf(e.Product) == StorageGroup.Common && f.Catalog.Exists(e.Material1));

        // The first material only, the gold of the recipe.
        f.Give(recipe.Material1, recipe.MaterialCount1);
        f.Gold = recipe.Gold;

        var items = f.Bag.Items.Select(e => (e.Serial, e.Count)).ToArray();

        await handler.OnHandleAsync(f.Context, new ItemSetupMakeRequest { Npc = f.NpcOffering(NpcFunction.Production, recipe.Group), MakeIndex = recipe.Id, MakeCount = 1 });

        CollectionAssert.AreEqual(items, f.Bag.Items.Select(e => (e.Serial, e.Count)).ToArray(), "The bag is as it was");
        Assert.AreEqual(recipe.Gold, f.Gold);
    }
}
