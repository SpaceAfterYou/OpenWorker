using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Domain.Types;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Npcs;
using OpenWorker.Gameplay.Modules.Npcs.Components;
using OpenWorker.Gameplay.Modules.Progress;
using OpenWorker.Gameplay.Modules.Shop;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.ItemSetup.Requests;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Shop.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The shops of the NPCs, the check of the NPC behind a service and the credit with groups of NPCs, against the tables of the
/// client (OPENWORKER_TEST_RES). Every NPC of the tables stands where the player stands unless a test moves the player.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class NpcServicesTest
{
    /// <summary>
    /// The least of every roll: the least credit of a raise.
    /// </summary>
    private sealed class LowestRandom : Random
    {
        public override int Next(int minValue, int maxValue) => minValue;
    }

    private sealed class Shops : IDisposable
    {
        public ItemSetupFixture F { get; } = new();
        public NpcCreditManager Credit { get; }
        public NpcShopService Shop { get; }
        public IReadOnlyList<ShopRow> Lines { get; } = ItemSetupFixture.Load<ShopRow>("tb_Shop");

        /// <param name="counting">Whether the person has chains of achievements for a purchase and for the credit with every group.</param>
        public Shops(bool counting = false)
        {
            Chains = counting
                ? new AchievementChains(
                    F.World,
                    [(AchievementType.ShopPurchase, 0), .. F.NpcTable.Select(e => (int)e.Group).Distinct().Select(e => (AchievementType.NpcCredit, e))])
                : null;

            Credit = new NpcCreditManager(
                F.World,
                new PersonStateStore(null!),
                new NpcCreditTables(
                    ItemSetupFixture.Load<CustomerGradeRow>("tb_Customer_Grade"),
                    ItemSetupFixture.Load<CustomerBenefitRow>("tb_Customer_Benefit"),
                    ItemSetupFixture.Load<CreditRaiseRow>("tb_Credit_Raise")),
                F.Inventory,
                new FakeTimeProvider(new DateTimeOffset(2026, 11, 9, 12, 0, 0, TimeSpan.Zero)),
                NullLogger<NpcCreditManager>.Instance,
                random: new LowestRandom(),
                achievements: Chains?.Manager);

            Shop = new NpcShopService(F.World, F.Inventory, F.Npcs, Credit, ItemSetupFixture.Load<ShopRow>("tb_Shop"), achievements: Chains?.Manager);

            F.World.Set(F.Player, new NpcCreditComponent { Person = 7, Book = new NpcCreditBook() });
            Chains?.Enter(F.Player);
        }

        public AchievementChains? Chains { get; }

        public NpcCreditBook Book => F.World.Get<NpcCreditComponent>(F.Player).Book!;

        /// <summary>
        /// A line for gold of the shop of an NPC, open to every grade, of an item the common bag takes, and that NPC.
        /// </summary>
        public (ShopRow Line, ItemRow Item, int Npc) GoldLine()
        {
            foreach (var line in Lines.Where(e => e is { PriceType: 1, CreditGrade: 0 }))
            {
                if (F.Catalog.TryGet(line.Item, out var item) && F.Catalog.HomeOf(line.Item) == StorageGroup.Common && item.Stack <= 1 &&
                    (line.Price != 0 ? line.Price : item.Price) >= item.SellPrice && (line.Price != 0 || item.Price > 0))
                {
                    return (line, item, F.NpcOffering(NpcFunction.Shop, line.Group));
                }
            }

            Assert.Fail("The shops sell nothing for gold");
            return default;
        }

        public void Dispose() => F.Dispose();
    }

    private static ActorValue Actor(int npc) => new(npc, Domain.Enums.ActorType.Npc);

    [TestMethod]
    public void APurchaseTakesThePriceOfTheLineAndGivesTheItemButNotWithoutTheMoneyOrTheRoom()
    {
        using var s = new Shops();
        var (line, item, npc) = s.GoldLine();
        var price = line.Price != 0 ? line.Price : item.Price;

        s.F.Gold = price;

        s.Shop.Buy(s.F.Player, Actor(npc), line.Id, 1);

        Assert.AreEqual(0, s.F.Gold, "the price of the line, in gold");
        Assert.AreEqual(1, s.F.Inventory.CountOf(s.F.Player, line.Item));

        // Without the money nothing changes.
        s.Shop.Buy(s.F.Player, Actor(npc), line.Id, 1);

        Assert.AreEqual(1, s.F.Inventory.CountOf(s.F.Player, line.Item), "nothing bought without the gold");

        // Nor without the room: the bag is full of other items.
        s.F.Gold = price;
        var filler = s.F.Weapon.Item;
        var room = s.F.Bag.Find(StorageGroup.Common)!.Slots.Count(e => e is null);

        Assert.IsTrue(s.F.Inventory.TryGive(s.F.Player, filler, room));

        s.Shop.Buy(s.F.Player, Actor(npc), line.Id, 1);

        Assert.AreEqual(price, s.F.Gold, "nothing paid for what does not fit");
        Assert.AreEqual(1, s.F.Inventory.CountOf(s.F.Player, line.Item));
    }

    [TestMethod]
    public void ASaleGivesThePriceOfTheItemRefusesWhatCannotBeSoldAndIsBoughtBack()
    {
        using var s = new Shops();
        var npc = s.F.NpcOffering(NpcFunction.Shop);

        var sold = s.F.Catalog.TryGet(800004001, out var row) ? row : default;
        Assert.IsTrue(sold.SellPrice > 0 && sold.SellLimit == 0, "an item a shop takes");

        var stack = s.F.Give(sold.Id, 3);
        var kept = s.F.Give(800000999, 1);

        s.Shop.Sell(s.F.Player, Actor(npc), [new ShopSellEntry(stack.Storage, stack.Slot), new ShopSellEntry(kept.Storage, kept.Slot)]);

        Assert.AreEqual(0, s.F.Gold, "a list with an item that cannot be sold sells nothing");
        Assert.AreEqual(3, s.F.Inventory.CountOf(s.F.Player, sold.Id));

        s.Shop.Sell(s.F.Player, Actor(npc), [new ShopSellEntry(stack.Storage, stack.Slot)]);

        Assert.AreEqual(3L * sold.SellPrice, s.F.Gold, "the whole stack for its sell price");
        Assert.AreEqual(0, s.F.Inventory.CountOf(s.F.Player, sold.Id));

        s.Shop.Repurchase(s.F.Player, Actor(npc), stack.ToValue());

        Assert.AreEqual(0, s.F.Gold, "bought back for what it was sold for");
        Assert.AreEqual(3, s.F.Inventory.CountOf(s.F.Player, sold.Id));
    }

    [TestMethod]
    public void ASaleThePurseCannotHoldIsRefusedWholeAndAFittingOneIsTaken()
    {
        using var s = new Shops();
        var npc = s.F.NpcOffering(NpcFunction.Shop);

        Assert.IsTrue(s.F.Catalog.TryGet(800004001, out var sold) && sold.SellPrice > 0);

        var stack = s.F.Give(sold.Id, 3);
        var price = 3L * sold.SellPrice;

        s.F.Gold = InventoryService.WalletLimit - price + 1;
        s.Shop.Sell(s.F.Player, Actor(npc), [new ShopSellEntry(stack.Storage, stack.Slot)]);

        Assert.AreEqual(InventoryService.WalletLimit - price + 1, s.F.Gold, "nothing was paid");
        Assert.AreEqual(3, s.F.Inventory.CountOf(s.F.Player, sold.Id), "and nothing was sold");

        s.F.Gold = InventoryService.WalletLimit - price;
        s.Shop.Sell(s.F.Player, Actor(npc), [new ShopSellEntry(stack.Storage, stack.Slot)]);

        Assert.AreEqual(InventoryService.WalletLimit, s.F.Gold, "a sale that just fits is taken");
    }

    [TestMethod]
    public async Task AServiceFarFromItsNpcOrOfAnNpcWithoutItIsRefusedAndChangesNothing()
    {
        using var s = new Shops();

        s.F.Weapon.Endurance = 50;
        s.F.Gold = 1_000_000;

        var repair = new ItemSetupRepairNpcRequest
        {
            Npc = s.F.NpcOffering(NpcFunction.Shop),
            Target = ItemSetupFixture.SlotOf(s.F.Weapon)
        };

        s.F.StandAway(ServiceNpcs.Reach + 1);
        await s.F.Handler.OnHandleAsync(s.F.Context, repair);

        Assert.AreEqual(50, s.F.Weapon.Endurance, "too far from the NPC");
        Assert.AreEqual(1_000_000, s.F.Gold);

        // Near, at an NPC that keeps no shop.
        s.F.StandAway(0);
        await s.F.Handler.OnHandleAsync(s.F.Context, repair with { Npc = s.F.NpcOffering(NpcFunction.Bank) });

        Assert.AreEqual(50, s.F.Weapon.Endurance, "an NPC that does not repair");
        Assert.AreEqual(1_000_000, s.F.Gold);

        await s.F.Handler.OnHandleAsync(s.F.Context, repair);

        Assert.IsTrue(s.F.Weapon.Endurance > 50, "near the NPC that repairs");
    }

    [TestMethod]
    public void EveryPurchaseCountsOnceForTheAchievementsAndTheCreditItEarnsCountsByItsGroup()
    {
        using var s = new Shops(counting: true);
        var (line, item, npc) = s.GoldLine();

        Assert.IsTrue(s.F.Npcs.Find(s.F.Player, Actor(npc), NpcFunction.Shop, out var at) == NpcRefusal.None);

        var price = line.Price != 0 ? line.Price : item.Price;

        s.F.Gold = price * 2L;
        s.Shop.Buy(s.F.Player, Actor(npc), line.Id, 1);

        var gained = s.Book.Find(at.Group)!.DayPoint;

        Assert.IsTrue(gained > 0, "the purchase gave credit");
        Assert.AreEqual(1, s.Chains!.Counted(s.F.Player, AchievementType.ShopPurchase));
        Assert.AreEqual(gained, s.Chains.Counted(s.F.Player, AchievementType.NpcCredit, at.Group), "the points of the group, not of another");

        s.Shop.Buy(s.F.Player, Actor(npc), line.Id, 1);

        Assert.AreEqual(2, s.Chains.Counted(s.F.Player, AchievementType.ShopPurchase));

        // A purchase that is refused counts for nothing.
        s.F.Gold = 0;
        s.Shop.Buy(s.F.Player, Actor(npc), line.Id, 1);

        Assert.AreEqual(2, s.Chains.Counted(s.F.Player, AchievementType.ShopPurchase));
    }

    [TestMethod]
    public void CreditRisesByAPurchaseAndItsGradeLowersAPrice()
    {
        using var s = new Shops();
        var (line, item, npc) = s.GoldLine();

        Assert.IsTrue(s.F.Npcs.Find(s.F.Player, Actor(npc), NpcFunction.Shop, out var at) == NpcRefusal.None);
        Assert.AreNotEqual(0, at.Group, "the NPC of the shop keeps credit");

        s.F.Gold = line.Price != 0 ? line.Price : item.Price;
        s.Shop.Buy(s.F.Player, Actor(npc), line.Id, 1);

        var credit = s.Book.Find(at.Group);

        Assert.IsNotNull(credit, "a purchase gives credit with the group of the NPC");
        Assert.IsTrue(credit.Point > 0 && credit.DayPoint == credit.Point);

        // A grade whose benefit takes a part off a repair at that group.
        credit.Grade = 1;
        var part = s.Credit.Benefit(s.F.Player, at, CreditBenefit.RepairMoney);

        Assert.IsTrue(part > 0, "the first grade of the group lowers repairs");
        Assert.AreEqual(1000 - 1000 * part / NpcCreditManager.Parts, s.Credit.Discount(s.F.Player, at, CreditBenefit.RepairMoney, 1000));
    }
}
