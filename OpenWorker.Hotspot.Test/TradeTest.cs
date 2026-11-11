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
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Messages.Response.Person.Enums;
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
        /// A player in the channel, near the others, with empty bags and the gold, and the trade password of its account given.
        /// </summary>
        public Player Join(long gold, PasswordProtectionState password = PasswordProtectionState.Authenticated)
        {
            var player = District.Join(100f, 100f);
            var bags = new Inventory(player.Actor);

            District.Ecs.Set(player.Entity, new TradePasswordComponent { Password = "hash", State = password });

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

    /// <summary>
    /// The answer with a code the player got to a request of the trade.
    /// </summary>
    private static async Task<PlayerFrame> RefusalAsync(Player player, TradeOpcode request)
    {
        await MetricAssertions.UntilAsync(() => player.Received().Any(e => e.Is(GroupOpcode.Trade, (byte)((byte)request | 0x80))), $"The player gets the refusal of {request}.");

        return player.Received().Single(e => e.Is(GroupOpcode.Trade, (byte)((byte)request | 0x80)));
    }

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

    /// <summary>
    /// The server asks for the trade password itself, not only the client: a player who has not given it (or is locked, or
    /// has none) cannot ask to trade, cannot take an invitation, and cannot open a shop. The refusals are the codes of the
    /// original, and nothing is left open on either side.
    /// </summary>
    [TestMethod]
    public async Task ATradeAndAShopNeedThePasswordGivenOnThisLogin()
    {
        using var market = new Market();

        var trusted = market.Join(gold: 100);
        var locked = market.Join(gold: 100, PasswordProtectionState.Lock);
        var forgetful = market.Join(gold: 100, PasswordProtectionState.UnAuthenticated);
        var cloth = market.Put(locked, 5, Cloth, 3);

        // Asking to trade: refused with the code of a request the state does not allow, whoever is asked.
        await market.Send(locked, Invite(trusted));

        Assert.AreEqual(TradeCodes.Refused, BitConverter.ToUInt16((await RefusalAsync(locked, TradeOpcode.Request)).Body, 0));
        Assert.IsFalse(market.Trades.IsTrading(locked.Entity) || market.Trades.IsTrading(trusted.Entity), "Nothing was opened.");
        Assert.IsFalse(trusted.Received().Any(e => e.Is(GroupOpcode.Trade, (byte)TradeOpcode.Request)), "The other player heard nothing.");

        // Taking an invitation: the answer goes to both as a refusal and the trade is closed.
        await market.Send(trusted, Invite(forgetful));
        await market.Send(forgetful, Accept(trusted));

        foreach (var player in new[] { trusted, forgetful })
        {
            var answer = await WaitForAsync(player, TradeOpcode.Accept);

            Assert.AreEqual((byte)0, answer.Body[0], "Both are told that the invitation was refused.");
        }

        Assert.IsFalse(market.Trades.IsTrading(trusted.Entity) || market.Trades.IsTrading(forgetful.Entity), "The refused invitation is over.");

        // Opening a shop: the code of a shop that cannot be made, and no sign over the head.
        await market.Send(locked, Read(w =>
        {
            w.Write((byte)StorageGroup.Common);
            w.Write((short)0);
            w.WriteUtf16UnicodeString("Cloth for sale");
            w.Write(new[] { new PrivateShopItemValue { Item = cloth.ToValue(), Money = 400 } });
        }, r => new TradePrivateShopStartRequest(r)));

        Assert.AreEqual(TradeCodes.ShopItemRefused, BitConverter.ToUInt16((await RefusalAsync(locked, TradeOpcode.PrivateShopStart)).Body, 0));
        Assert.IsNull(market.Shops.ShopOf(locked.Entity));
        Assert.AreEqual(PrivateShops.Closed, market.District.Ecs.Get<ShopPrivateComponent>(locked.Entity).Type);
    }

    /// <summary>
    /// An item that is bound to its owner, or broken, is never put on the table of a trade, nor into a shop of a player; a
    /// free one is. One rule (<c>ItemBinding.HeldBack</c>) decides for both, and a broken item is asked before a bound one.
    /// </summary>
    [TestMethod]
    public async Task ABoundOrBrokenItemStaysOffTheTableAndOutOfAShop()
    {
        using var market = new Market();

        var seller = market.Join(gold: 100);
        var buyer = market.Join(gold: 100);
        var bound = market.Put(seller, 0, Cloth, 1);
        var broken = market.Put(seller, 1, Cloth, 1);
        var free = market.Put(seller, 2, Cloth, 1);

        bound.BindState = ItemBindState.BoundAndBroach;
        broken.Flag = 2;
        broken.BindState = ItemBindState.Bound;

        await market.Send(seller, Invite(buyer));
        await WaitForAsync(buyer, TradeOpcode.Request);
        await market.Send(buyer, Accept(seller));
        await WaitForAsync(seller, TradeOpcode.Accept);

        async Task<ushort> RefusedAsync(ItemInstance item, int refusals)
        {
            await market.Send(seller, Offer(item));
            await MetricAssertions.UntilAsync(
                () => seller.Received().Count(e => e.Is(GroupOpcode.Trade, (byte)TradeOpcode.UpdateItem | 0x80)) == refusals,
                "The player gets the refusal.");

            return BitConverter.ToUInt16(seller.Received().Last(e => e.Is(GroupOpcode.Trade, (byte)TradeOpcode.UpdateItem | 0x80)).Body, 0);
        }

        Assert.AreEqual(TradeCodes.ItemBound, await RefusedAsync(bound, 1), "A bound item, whatever binds it.");
        Assert.AreEqual(TradeCodes.ItemRefused, await RefusedAsync(broken, 2), "A broken item is refused as the one that is not on the list.");

        await market.Send(seller, Offer(free));
        await WaitForAsync(seller, TradeOpcode.UpdateItem);

        Assert.AreEqual(2, seller.Received().Count(e => e.Is(GroupOpcode.Trade, (byte)TradeOpcode.UpdateItem | 0x80)), "The free item was taken.");

        // The same rule keeps them out of a shop of a player; a free item opens one.
        var keeper = market.Join(gold: 100);
        var boundGoods = market.Put(keeper, 0, Cloth, 1);
        var freeGoods = market.Put(keeper, 1, Cloth, 1);

        boundGoods.BindState = ItemBindState.Bound;

        TradePrivateShopStartRequest Shop(ItemInstance goods) => Read(w =>
        {
            w.Write((byte)StorageGroup.Common);
            w.Write((short)0);
            w.WriteUtf16UnicodeString("Cloth for sale");
            w.Write(new[] { new PrivateShopItemValue { Item = goods.ToValue(), Money = 400 } });
        }, r => new TradePrivateShopStartRequest(r));

        await market.Send(keeper, Shop(boundGoods));

        Assert.AreEqual(TradeCodes.ShopItemRefused, BitConverter.ToUInt16((await RefusalAsync(keeper, TradeOpcode.PrivateShopStart)).Body, 0));
        Assert.IsNull(market.Shops.ShopOf(keeper.Entity));

        await market.Send(keeper, Shop(freeGoods));

        Assert.IsNotNull(market.Shops.ShopOf(keeper.Entity));
    }
}
