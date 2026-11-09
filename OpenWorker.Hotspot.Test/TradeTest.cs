using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Channel.Community;
using OpenWorker.DistrictServer.Server;
using OpenWorker.DistrictServer.Services;
using OpenWorker.DistrictServer.Trade;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Persons.Extensions;
using OpenWorker.Hotspot.Modules.Trade.Extensions;
using OpenWorker.Hotspot.Modules.Trade.Requests;
using OpenWorker.Hotspot.Modules.Trade.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// Trades and private shops through the handlers of the district, between players of one channel whose connections record
/// what they get. The bags are checked before and after.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class TradeTest
{
    // Any number is an item to an empty catalog, which keeps every item in the common bag, one per slot.
    private const int Cloth = 710105001;
    private const int Gem = 720000001;

    private sealed class NobodyBlocked : IPersonRelations
    {
        public ValueTask<bool> IsFriendAsync(int person, int other) => ValueTask.FromResult(false);

        public ValueTask<bool> IsBlockedAsync(int person, int other) => ValueTask.FromResult(false);
    }

    /// <summary>
    /// A district with one channel and the trade handlers, as the server builds them.
    /// </summary>
    private sealed class Market : IDisposable
    {
        private static ReadOnlyCollection<T> Empty<T>() => new([]);

        private long _serial = 500;

        public Market()
        {
            var catalog = new ItemCatalog(Empty<ItemRow>(), Empty<ItemClassifyRow>(), Empty<InvenSlotExtendRow>(), Empty<BankSlotExtendRow>());

            Inventory = new InventoryService(District.Ecs, catalog, new LocalItemSerials());

            var goods = new TradeGoods(District.Ecs, Inventory);

            Trades = new Trades(District.Ecs, District.Channels, goods, NoPersonFlush.Instance, District.Time, NullLogger<Trades>.Instance);
            Shops = new PrivateShops(District.Ecs, Trades, goods, new ChannelAudience(District.Ecs, District.Channels), NoPersonFlush.Instance,
                NullLogger<PrivateShops>.Instance);
            Service = new TradeService(District.Ecs, District.Channels, Trades, Shops, new NobodyBlocked(), District.Errors);
        }

        public DistrictHarness District { get; } = new();

        public InventoryService Inventory { get; }

        public Trades Trades { get; }

        public PrivateShops Shops { get; }

        public TradeService Service { get; }

        /// <summary>
        /// A player in the channel, near the others, with empty bags and the gold.
        /// </summary>
        public Player Join(long gold)
        {
            var player = District.Join(100f, 100f);
            var bags = new Inventory(player.Actor);

            bags.Add(StorageGroup.Common, 24);
            bags.Add(StorageGroup.Costume, 24);

            District.Ecs.Set(player.Entity, new InventoryComponent(bags));
            District.Ecs.Set(player.Entity, new CurrencyComponent { Gold = gold });

            return player;
        }

        public Inventory BagsOf(Player player) => Inventory.InventoryOf(player.Entity)!;

        public long GoldOf(Player player) => Inventory.Gold(player.Entity);

        public ItemInstance Put(Player player, short slot, int item, short count)
        {
            var instance = new ItemInstance { Serial = ++_serial, Item = item, Count = count };

            BagsOf(player).Place(StorageGroup.Common, slot, instance);

            return instance;
        }

        public ValueTask Send<T>(Player player, T request)
        {
            var context = new ServiceHandleContext(player.Entity, CancellationToken.None);

            return request switch
            {
                TradeRequest e => Service.OnHandleAsync(context, e),
                TradeAcceptRequest e => Service.OnHandleAsync(context, e),
                TradeUpdateItemRequest e => Service.OnHandleAsync(context, e),
                TradeUpdateMoneyRequest e => Service.OnHandleAsync(context, e),
                TradeCheckRequest e => Service.OnHandleAsync(context, e),
                TradeConfirmRequest e => Service.OnHandleAsync(context, e),
                TradePrivateShopStartRequest e => Service.OnHandleAsync(context, e),
                TradePrivateShopSelectRequest e => Service.OnHandleAsync(context, e),
                TradePrivateShopItemRequest e => Service.OnHandleAsync(context, e),
                _ => throw new ArgumentException("Not a request of the trade.", nameof(request))
            };
        }

        public void Dispose() => District.Dispose();
    }

    private static T Read<T>(Action<BinaryWriter> write, Func<BinaryReader, T> read)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            write(writer);
        }

        stream.Position = 0;

        using var reader = new BinaryReader(stream);

        return read(reader);
    }

    private static int Raw(Player player) => new ActorValue(player.Actor, ActorType.User);

    private static TradeRequest Invite(Player target) => Read(w => w.WritePersonName("Player" + (target.Actor - 1000)), r => new TradeRequest(r));

    private static TradeAcceptRequest Accept(Player inviter) => Read(w => { w.Write(Raw(inviter)); w.Write((byte)1); }, r => new TradeAcceptRequest(r));

    private static TradeUpdateItemRequest Offer(ItemInstance item) =>
        Read(w => { w.Write((byte)1); w.Write((byte)item.Storage); w.Write(item.Slot); }, r => new TradeUpdateItemRequest(r));

    private static TradeUpdateMoneyRequest Gold(long money) => Read(w => w.Write(money), r => new TradeUpdateMoneyRequest(r));

    private static TradeCheckRequest Lock() => Read(w => w.Write((byte)1), r => new TradeCheckRequest(r));

    private static async Task<PlayerFrame> WaitForAsync(Player player, TradeOpcode command)
    {
        await MetricAssertions.UntilAsync(() => player.Received().Any(e => e.Is(GroupOpcode.Trade, (byte)command)), $"The player gets {command}.");

        return player.Received().Last(e => e.Is(GroupOpcode.Trade, (byte)command));
    }

    /// <summary>
    /// Two players open a trade, each puts an item on the table and one adds gold, both lock and confirm: the items change
    /// bags, the gold changes purses, and both are told it is done.
    /// </summary>
    [TestMethod]
    public async Task ATradeMovesTheItemsAndTheGoldOfBothSides()
    {
        using var market = new Market();

        var seller = market.Join(gold: 1_000);
        var buyer = market.Join(gold: 50);
        var cloth = market.Put(seller, 3, Cloth, 7);
        var gem = market.Put(buyer, 0, Gem, 1);

        await market.Send(seller, Invite(buyer));
        await WaitForAsync(buyer, TradeOpcode.Request);

        await market.Send(buyer, Accept(seller));
        await WaitForAsync(seller, TradeOpcode.Accept);

        await market.Send(seller, Offer(cloth));
        await market.Send(seller, Gold(300));
        await market.Send(buyer, Offer(gem));

        await market.Send(seller, Lock());
        await market.Send(buyer, Lock());
        await market.Send(seller, new TradeConfirmRequest());
        await market.Send(buyer, new TradeConfirmRequest());

        Assert.AreEqual((byte)TradeResult.Done, (await WaitForAsync(seller, TradeOpcode.Result)).Body[0]);
        Assert.AreEqual((byte)TradeResult.Done, (await WaitForAsync(buyer, TradeOpcode.Result)).Body[0]);

        Assert.IsFalse(market.BagsOf(seller).Items.Any(e => e.Serial == cloth.Serial), "The cloth left the bags of the seller.");
        Assert.IsFalse(market.BagsOf(buyer).Items.Any(e => e.Serial == gem.Serial), "The gem left the bags of the buyer.");

        var got = market.BagsOf(buyer).Items.Single();
        var paid = market.BagsOf(seller).Items.Single();

        Assert.AreEqual((Cloth, (short)7), (got.Item, got.Count));
        Assert.AreEqual(Gem, paid.Item);
        Assert.AreNotEqual(cloth.Serial, got.Serial, "The item lives on under a serial of the new owner.");

        Assert.AreEqual(700, market.GoldOf(seller));
        Assert.AreEqual(350, market.GoldOf(buyer));
        Assert.IsFalse(market.Trades.IsTrading(seller.Entity) || market.Trades.IsTrading(buyer.Entity));
    }

    /// <summary>
    /// An item offered and locked leaves the bags before the second confirmation: the exchange finds it gone, moves
    /// nothing, and both sides are told the trade is off.
    /// </summary>
    [TestMethod]
    public async Task AnItemGoneBeforeTheConfirmationStopsTheExchange()
    {
        using var market = new Market();

        var seller = market.Join(gold: 0);
        var buyer = market.Join(gold: 500);
        var cloth = market.Put(seller, 3, Cloth, 7);

        await market.Send(seller, Invite(buyer));
        await market.Send(buyer, Accept(seller));
        await market.Send(seller, Offer(cloth));
        await market.Send(buyer, Gold(500));
        await market.Send(seller, Lock());
        await market.Send(buyer, Lock());
        await market.Send(seller, new TradeConfirmRequest());

        // The seller throws part of the stack away with the trade window open.
        market.Inventory.Break(seller.Entity, StorageGroup.Common, 3, cloth.Serial, 2);

        await market.Send(buyer, new TradeConfirmRequest());

        foreach (var player in new[] { seller, buyer })
        {
            var cancel = await WaitForAsync(player, TradeOpcode.Cancel);

            Assert.AreEqual(TradeCodes.ItemsUnavailable, BitConverter.ToInt32(cancel.Body, 4));
            Assert.IsFalse(player.Received().Any(e => e.Is(GroupOpcode.Trade, (byte)TradeOpcode.Result)));
        }

        Assert.AreEqual((short)5, market.BagsOf(seller).At(StorageGroup.Common, 3)!.Count);
        Assert.IsEmpty(market.BagsOf(buyer).Items.ToList());
        Assert.AreEqual(0, market.GoldOf(seller));
        Assert.AreEqual(500, market.GoldOf(buyer));
        Assert.IsFalse(market.Trades.IsTrading(seller.Entity) || market.Trades.IsTrading(buyer.Entity));
    }

    /// <summary>
    /// A player opens a shop with one item; another opens the shop and buys it at the price it was shown: the item and
    /// the gold move, the owner is told of the sale, and the empty shop closes.
    /// </summary>
    [TestMethod]
    public async Task APrivateShopSellsItsItem()
    {
        using var market = new Market();

        var owner = market.Join(gold: 10);
        var customer = market.Join(gold: 1_000);
        var cloth = market.Put(owner, 5, Cloth, 3);

        await market.Send(owner, Read(w =>
        {
            w.Write((byte)StorageGroup.Common);
            w.Write((short)0);
            w.WriteUtf16UnicodeString("Cloth for sale");
            w.Write(new[] { new PrivateShopItemValue { Item = cloth.ToValue(), Money = 400 } });
        }, r => new TradePrivateShopStartRequest(r)));

        Assert.AreEqual(PrivateShops.Selling, market.District.Ecs.Get<ShopPrivateComponent>(owner.Entity).Type);

        await market.Send(customer, Read(w => w.Write(Raw(owner)), r => new TradePrivateShopSelectRequest(r)));
        await WaitForAsync(customer, TradeOpcode.PrivateShopSelect);

        // The first line, at the price the customer was shown.
        await market.Send(customer, Read(w => { w.Write((byte)StorageGroup.Common); w.Write((short)0); w.Write(400L); },
            r => new TradePrivateShopItemRequest(r)));

        await WaitForAsync(customer, TradeOpcode.PrivateShopBuy);
        await WaitForAsync(owner, TradeOpcode.PrivateShopSell);

        Assert.IsNull(market.BagsOf(owner).At(StorageGroup.Common, 5));
        Assert.AreEqual((Cloth, (short)3), (market.BagsOf(customer).Items.Single().Item, market.BagsOf(customer).Items.Single().Count));
        Assert.AreEqual(410, market.GoldOf(owner));
        Assert.AreEqual(600, market.GoldOf(customer));

        Assert.AreEqual(PrivateShops.Closed, market.District.Ecs.Get<ShopPrivateComponent>(owner.Entity).Type, "A shop with nothing left closes.");
        Assert.IsNull(market.Shops.ShopOf(owner.Entity));
    }
}
