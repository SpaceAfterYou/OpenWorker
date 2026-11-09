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

    /// <summary>
    /// The akashic cards of the client's table that lie in the common bag, by id.
    /// </summary>
    private static Dictionary<int, AkashicRecordsRow> Cards(ItemSetupFixture f)
    {
        return ItemSetupFixture.Load<AkashicRecordsRow>("tb_Akashic_Records")
            .Where(e => f.Catalog.Exists(e.Id) && f.Catalog.HomeOf(e.Id) == StorageGroup.Common)
            .GroupBy(e => e.Id)
            .ToDictionary(e => e.Key, e => e.First());
    }

    [TestMethod]
    public async Task MakingACardTakesAFullCardItsExtraCardsAndGoldAndGivesTheCardOfTheRecipe()
    {
        using var f = new ItemSetupFixture();

        var cards = Cards(f);

        // A recipe that makes the next card of a full card, with extra cards of the same rarity.
        var recipe = ItemSetupFixture.Load<AkashicMakeRow>("tb_Akashic_Make").First(e =>
            e.MaterialKind == AkashicService.CardMaterial && e.ExtraCount > 0 && e.Card == 0 &&
            cards.TryGetValue(e.Material, out var row) && row.MakeExp > 0 && cards.ContainsKey(e.Result) &&
            cards.Values.Count(x => x.RarePoint == row.RarePoint && x.Id != e.Material && x.Id != e.Result) >= e.ExtraCount);
        var made = cards[recipe.Material];

        var material = f.Give(recipe.Material, 1);
        var extras = cards.Values
            .Where(e => e.RarePoint == made.RarePoint && e.Id != recipe.Material && e.Id != recipe.Result)
            .Take(recipe.ExtraCount)
            .Select(e => f.Give(e.Id, 1))
            .ToList();

        material.Exp = made.MakeExp;
        f.Gold = recipe.Gold + 5;

        await f.Handler.OnHandleAsync(f.Context, new ItemSetupAkashicMakeExRequest
        {
            Index = recipe.Id,
            Npc = 1,
            Material = ItemSetupFixture.SlotOf(material),
            Extras = extras.Select(ItemSetupFixture.SlotOf).ToList()
        });

        Assert.AreEqual(1, f.Inventory.CountOf(f.Player, recipe.Result), "The card of the recipe is made");
        Assert.IsFalse(f.Bag.Items.Any(e => e.Serial == material.Serial || extras.Any(x => x.Serial == e.Serial)),
            "The full card and the extra cards are used up");
        Assert.AreEqual(5, f.Gold, "The gold of the recipe is charged");
    }

    [TestMethod]
    public async Task ComposingFeedsCardsToACardUpToWhatMakingTheNextNeeds()
    {
        using var f = new ItemSetupFixture();

        var cards = Cards(f);

        var target = cards.Values.First(e => e.MakeExp > 1);
        var fodder = cards.Values.Where(e => e.ExpGiven > 0 && e.Id != target.Id).Take(2).ToList();

        var card = f.Give(target.Id, 1);
        var first = f.Give(fodder[0].Id, 1);

        await f.Handler.OnHandleAsync(f.Context, new ItemSetupAkashicComposeRequest
        {
            Npc = 1,
            Target = ItemSetupFixture.SlotOf(card),
            Materials = [ItemSetupFixture.SlotOf(first)]
        });

        Assert.AreEqual(Math.Min(fodder[0].ExpGiven, target.MakeExp), card.Exp, "The card gains the experience of the card fed to it");
        Assert.IsFalse(f.Bag.Items.Any(e => e.Serial == first.Serial), "The card fed to it is used up");

        card.Exp = target.MakeExp - 1;

        var second = f.Give(fodder[1].Id, 1);

        await f.Handler.OnHandleAsync(f.Context, new ItemSetupAkashicComposeRequest
        {
            Npc = 1,
            Target = ItemSetupFixture.SlotOf(card),
            Materials = [ItemSetupFixture.SlotOf(second)]
        });

        Assert.AreEqual(target.MakeExp, card.Exp, "The card stops at what making the next card needs");
        Assert.IsTrue(f.Dirty.Sections.HasFlag(PersonSections.Inventory), "The card is written by the flush");
    }

    [TestMethod]
    public async Task AUsedCardGoesIntoTheBookAndComesBackOutWithItsExperienceForThePriceOfTheTable()
    {
        using var f = new ItemSetupFixture();

        var cards = Cards(f);
        var backs = ItemSetupFixture.Load<AkashicDisassembleRow>("tb_Akashic_Disassemble")
            .GroupBy(e => e.Id)
            .ToDictionary(e => e.Key, e => e.First());

        var row = cards.Values.First(e => backs.TryGetValue(e.Disassemble, out var back) && back.CostKind == 0 && f.Catalog.Exists(back.Item));
        var back = backs[row.Disassemble];

        var card = f.Give(row.Id, 1);

        card.Exp = 17;

        var use = new ItemUseService(f.Inventory, f.Catalog, new([]), new([]), null, f.Akashic);

        Assert.IsTrue(await use.UseAsync(f.Player, card.Storage, card.Slot, card.Serial, CancellationToken.None));

        var record = f.Bag.AkashicRecords.Records.Single();

        Assert.AreEqual((row.Id, (int)row.Position, 17), (record.Card, record.Position, record.Exp), "The card is in the book at its place");
        Assert.IsFalse(f.Bag.Items.Any(e => e.Serial == card.Serial), "The card left the bag");
        Assert.IsTrue(f.Dirty.Sections.HasFlag(PersonSections.AkashicRecords), "The book is written by the flush");

        f.Gold = back.Cost + 3;

        await f.Handler.OnHandleAsync(f.Context, new ItemSetupAkashicDisassembleRequest { Npc = 1, Card = row.Id });

        Assert.AreEqual(0, f.Bag.AkashicRecords.Records.Count, "The card left the book");
        Assert.AreEqual(17, f.Bag.Items.Single(e => e.Item == back.Item).Exp, "The card comes back with its experience");
        Assert.AreEqual(3, f.Gold, "The price of the table is charged");
    }

    [TestMethod]
    public async Task ARefusedAkashicRequestChangesNothing()
    {
        using var f = new ItemSetupFixture();

        var cards = Cards(f);

        // A recipe without extra cards whose card is not yet full.
        var recipe = ItemSetupFixture.Load<AkashicMakeRow>("tb_Akashic_Make").First(e =>
            e.MaterialKind == AkashicService.CardMaterial && e.ExtraCount == 0 && cards.TryGetValue(e.Material, out var row) &&
            row.MakeExp > 0 && f.Catalog.Exists(e.Result));
        var material = f.Give(recipe.Material, 1);

        if (recipe.Card != 0)
        {
            f.Give(recipe.Card, 1);
        }

        material.Exp = cards[recipe.Material].MakeExp - 1;
        f.Gold = recipe.Gold;

        await f.Handler.OnHandleAsync(f.Context, new ItemSetupAkashicMakeRequest
        {
            Npc = 1,
            Index = recipe.Id,
            Material = ItemSetupFixture.SlotOf(material)
        });

        Assert.AreEqual(0, f.Inventory.CountOf(f.Player, recipe.Result), "Nothing is made");
        Assert.AreSame(material, f.Bag.At(material.Storage, material.Slot), "The card stays");
        Assert.AreEqual(recipe.Gold, f.Gold, "No gold is charged");

        // A card that is not in the book cannot be taken out of it.
        await f.Handler.OnHandleAsync(f.Context, new ItemSetupAkashicDisassembleRequest { Npc = 1, Card = recipe.Material });

        Assert.AreEqual(1, f.Inventory.CountOf(f.Player, recipe.Material), "Nothing comes out of an empty book");
        Assert.AreEqual(recipe.Gold, f.Gold);
    }
}
