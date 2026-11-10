using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Hotspot.Modules.Trade.Responses;
using OpenWorker.Hotspot.Modules.Trade.Types;
using OpenWorker.Server.Common;

namespace OpenWorker.DistrictServer.Trade;

/// <summary>
/// One line of a private shop: an item of the bags of the owner and its price. The item stays in the bags until it is
/// sold, and a sale takes it only if it is still the same.
/// </summary>
public readonly record struct PrivateShopLine(TradeOffer Item, long Price);

/// <summary>
/// The shop a player keeps in the street: its title, its lines, and whether it sells now or is stopped for changes.
/// </summary>
public sealed class PrivateShop(Entity owner, ActorValue actor, string title)
{
    public Entity Owner { get; } = owner;

    public ActorValue Actor { get; } = actor;

    public string Title { get; } = title;

    public List<PrivateShopLine> Lines { get; } = [];

    public byte State { get; set; } = PrivateShops.Selling;

    public int IndexOf(StorageGroup storage, short slot)
    {
        return Lines.FindIndex(e => e.Item.Storage == storage && e.Item.Slot == slot);
    }
}

/// <summary>
/// The private shops of this district. A player opens a shop with up to five items of its bags and their prices; the
/// players of its channel see the sign over its head (<see cref="ShopPrivateComponent"/>), open the shop and buy. The shop
/// closes when its owner closes it, sells the last item or leaves. Everything runs on the world loop, and a sale is one
/// synchronous block that checks the item, the gold and the room and then moves them.
/// </summary>
public sealed partial class PrivateShops(
    World world,
    Trades trades,
    TradeGoods goods,
    IPlayerAudience audience,
    IPersonFlush persons,
    ILogger<PrivateShops> logger)
{
    /// <summary>The state of a shop as the players around see it: none.</summary>
    public const byte Closed = 0;

    /// <summary>The owner has stopped the shop to change it; nobody can buy.</summary>
    public const byte Stopped = 1;

    /// <summary>The shop sells.</summary>
    public const byte Selling = 2;

    public const int MaxLines = 5;

    /// <summary>
    /// The longest title the client keeps.
    /// </summary>
    public const int MaxTitleLength = 20;

    private readonly Dictionary<Entity, PrivateShop> _shops = [];
    private readonly Dictionary<int, PrivateShop> _byActor = [];

    // The shop each player last opened and the items it was shown there, in their order: a purchase names one of them.
    private readonly Dictionary<Entity, Browsing> _browsing = [];

    private readonly record struct Browsing(Entity Owner, long[] Serials);

    public PrivateShop? ShopOf(Entity owner) => _shops.GetValueOrDefault(owner);

    /// <summary>
    /// The player opens its shop with the title and the items of its bags, each with its price.
    /// </summary>
    public void Start(Entity player, string title, IReadOnlyList<PrivateShopItemValue> items)
    {
        if (_shops.ContainsKey(player))
        {
            Refuse(player, TradeOpcode.PrivateShopStart, TradeCodes.ShopSelling);
            return;
        }

        // A shop stands in a channel, and its owner does nothing else meanwhile.
        if (trades.IsTrading(player) || !trades.InSameChannel(player, player))
        {
            Refuse(player, TradeOpcode.PrivateShopStart, TradeCodes.Busy);
            return;
        }

        // The password of the account must be given on this login, or the shop is refused with the code the original gives.
        if (!trades.MayTrade(player))
        {
            Refuse(player, TradeOpcode.PrivateShopStart, TradeCodes.ShopItemRefused);
            return;
        }

        if (string.IsNullOrWhiteSpace(title) || title.Length > MaxTitleLength || items.Count is 0 or > MaxLines)
        {
            Refuse(player, TradeOpcode.PrivateShopStart, TradeCodes.ShopItemRefused);
            return;
        }

        var shop = new PrivateShop(player, world.Get<ActorComponent>(player), title);

        foreach (var value in items)
        {
            // The client names an item of its bags by its serial.
            if (value.Money <= 0 || Find(player, value.Item.Serial.Number) is not { } item ||
                goods.Offerable(player, item.Storage, item.Slot, out _) != 0 ||
                shop.IndexOf(item.Storage, item.Slot) >= 0)
            {
                Refuse(player, TradeOpcode.PrivateShopStart, TradeCodes.ShopItemRefused);
                return;
            }

            shop.Lines.Add(new PrivateShopLine(TradeGoods.Describe(item), value.Money));
        }

        _shops[player] = shop;
        _byActor[shop.Actor.Identifier] = shop;

        world.Set(player, new ShopPrivateComponent { Type = Selling, Name = title });

        audience.Nearby(player, new TradePrivateShopStartResponse { Actor = shop.Actor, Title = title });
    }

    /// <summary>
    /// The owner stops its shop to change it, opens it again, or closes it.
    /// </summary>
    public void ChangeState(Entity player, byte state)
    {
        if (ShopOf(player) is not { } shop)
        {
            Refuse(player, TradeOpcode.PrivateShopState, TradeCodes.NoShop);
            return;
        }

        switch (state)
        {
            case Closed:
            {
                Close(shop);
                return;
            }

            case Stopped when shop.State != Selling:
            {
                Refuse(player, TradeOpcode.PrivateShopState, TradeCodes.ShopNotSelling);
                return;
            }

            case Selling when shop.State == Selling:
            {
                Refuse(player, TradeOpcode.PrivateShopState, TradeCodes.ShopSelling);
                return;
            }

            case Selling when shop.Lines.Count == 0:
            {
                Refuse(player, TradeOpcode.PrivateShopState, TradeCodes.NoShop);
                return;
            }

            case Stopped or Selling:
            {
                Show(shop, state);
                return;
            }

            default:
            {
                Refuse(player, TradeOpcode.PrivateShopState, TradeCodes.NoShop);
                return;
            }
        }
    }

    /// <summary>
    /// A line of a shop: the owner of a stopped shop sets the price of an item of its bags (zero takes the item out);
    /// anybody else buys the line of the shop it opened last (<see cref="Buy"/>).
    /// </summary>
    public ValueTask Item(Entity player, StorageSlot slot, long money)
    {
        if (ShopOf(player) is not { } shop)
        {
            return Buy(player, slot.Slot, money);
        }

        if (shop.State == Selling)
        {
            Refuse(player, TradeOpcode.PrivateShopItem, TradeCodes.ShopSelling);
            return ValueTask.CompletedTask;
        }

        var index = shop.IndexOf(slot.Storage, slot.Slot);

        if (money == 0)
        {
            if (index < 0)
            {
                Refuse(player, TradeOpcode.PrivateShopItem, TradeCodes.ShopFull);
                return ValueTask.CompletedTask;
            }

            shop.Lines.RemoveAt(index);
        }

        else
        {
            if (money < 0 || goods.Offerable(player, slot.Storage, slot.Slot, out var item) != 0)
            {
                Refuse(player, TradeOpcode.PrivateShopItem, TradeCodes.ShopItemRefused);
                return ValueTask.CompletedTask;
            }

            var line = new PrivateShopLine(TradeGoods.Describe(item), money);

            if (index >= 0)
            {
                shop.Lines[index] = line;
            }

            else if (shop.Lines.Count >= MaxLines)
            {
                Refuse(player, TradeOpcode.PrivateShopItem, TradeCodes.ShopFull);
                return ValueTask.CompletedTask;
            }

            else
            {
                shop.Lines.Add(line);
            }
        }

        Send(player, new TradePrivateShopItemResponse { Slot = slot, Money = money });

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// The player opens the shop of another one and gets its lines.
    /// </summary>
    public void Select(Entity player, ActorValue owner)
    {
        if (!_byActor.TryGetValue(owner.Identifier, out var shop) || shop.Owner == player || !trades.InSameChannel(player, shop.Owner))
        {
            Refuse(player, TradeOpcode.PrivateShopSelect, TradeCodes.ShopGone);
            return;
        }

        if (shop.State != Selling)
        {
            Refuse(player, TradeOpcode.PrivateShopSelect, TradeCodes.ShopNotSelling);
            return;
        }

        // A line whose item has left the bags since is not shown: it cannot be sold.
        var lines = shop.Lines.Where(e => goods.StillHolds(shop.Owner, e.Item)).ToList();

        if (lines.Count == 0)
        {
            Refuse(player, TradeOpcode.PrivateShopSelect, TradeCodes.NoShop);
            return;
        }

        var bags = goods.Inventory.InventoryOf(shop.Owner)!;

        _browsing[player] = new Browsing(shop.Owner, lines.Select(e => e.Item.Serial).ToArray());

        Send(player, new TradePrivateShopSelectResponse
        {
            Actor = shop.Actor,
            Items = lines
                .Select(e => new PrivateShopItemValue { Item = bags.At(e.Item.Storage, e.Item.Slot)!.ToValue(), Money = e.Price })
                .ToArray()
        });
    }

    /// <summary>
    /// The player buys a line of the shop it opened last, named by its place in the list it was shown, at the price it was
    /// shown: the item goes into its bags, the gold to the owner, and those who look at the shop see the line go.
    /// </summary>
    public ValueTask Buy(Entity player, short shown, long price)
    {
        if (!_browsing.TryGetValue(player, out var browsing) || ShopOf(browsing.Owner) is not { } shop ||
            !trades.InSameChannel(player, browsing.Owner))
        {
            Refuse(player, TradeOpcode.PrivateShopItem, TradeCodes.ShopGone);
            return ValueTask.CompletedTask;
        }

        if (shop.State != Selling)
        {
            Refuse(player, TradeOpcode.PrivateShopItem, TradeCodes.ShopNotSelling);
            return ValueTask.CompletedTask;
        }

        var owner = browsing.Owner;
        var line = shown >= 0 && shown < browsing.Serials.Length ? shop.Lines.FindIndex(e => e.Item.Serial == browsing.Serials[shown]) : -1;

        // A line sold since, changed by the owner, or whose item has left the bags.
        if (line < 0 || shop.Lines[line].Price != price || !goods.StillHolds(owner, shop.Lines[line].Item))
        {
            Refuse(player, TradeOpcode.PrivateShopItem, TradeCodes.ShopItemRefused);
            return ValueTask.CompletedTask;
        }

        var sold = shop.Lines[line];

        if (goods.Inventory.Gold(player) < price)
        {
            Refuse(player, TradeOpcode.PrivateShopItem, TradeCodes.NotEnoughGold);
            return ValueTask.CompletedTask;
        }

        if (!goods.CanReceive(owner, price))
        {
            Refuse(player, TradeOpcode.PrivateShopItem, TradeCodes.TooMuchGold);
            return ValueTask.CompletedTask;
        }

        if (!goods.HasRoom(player, [sold.Item], []))
        {
            Refuse(player, TradeOpcode.PrivateShopItem, TradeCodes.ItemsUnavailable);
            return ValueTask.CompletedTask;
        }

        // Everything is checked: from here on nothing can fail.
        shop.Lines.RemoveAt(line);

        goods.Give(player, goods.Take(owner, [sold.Item]));
        goods.Inventory.TryAddGold(player, -price);
        goods.Inventory.TryAddGold(owner, price);

        audience.Nearby(owner, new TradePrivateShopSellResponse { Actor = shop.Actor, Serial = sold.Item.Serial });

        Send(player, new TradePrivateShopBuyResponse
        {
            Serial = sold.Item.Serial,
            Name = world.Get<PersonInfoComponent>(owner).Name,
            Item = sold.Item.Item
        });

        var buyer = world.Get<ActorComponent>(player);

        LogSold(logger, shop.Actor.Identifier, sold.Item.Item, price, buyer.Identifier);

        if (shop.Lines.Count == 0)
        {
            Close(shop);
        }

        return FlushAsync(player, owner);
    }

    /// <summary>
    /// The connection of the player closes: its shop closes, and it no longer looks at another one.
    /// </summary>
    public void Leave(Entity player)
    {
        _browsing.Remove(player);

        if (ShopOf(player) is { } shop)
        {
            Close(shop);
        }
    }

    private void Close(PrivateShop shop)
    {
        _shops.Remove(shop.Owner);
        _byActor.Remove(shop.Actor.Identifier);

        foreach (var browser in _browsing.Where(e => e.Value.Owner == shop.Owner).Select(e => e.Key).ToList())
        {
            _browsing.Remove(browser);
        }

        Show(shop, Closed);
    }

    /// <summary>
    /// Sets the state of the shop and shows it to the players around, the owner too.
    /// </summary>
    private void Show(PrivateShop shop, byte state)
    {
        shop.State = state;

        if (!world.IsAlive(shop.Owner))
        {
            return;
        }

        world.Set(shop.Owner, new ShopPrivateComponent { Type = state, Name = state == Closed ? string.Empty : shop.Title });

        audience.Nearby(shop.Owner, new TradePrivateShopStateResponse { Actor = shop.Actor, State = state });
    }

    /// <summary>
    /// The item of the bags of the player with the serial.
    /// </summary>
    private ItemInstance? Find(Entity player, long serial)
    {
        return goods.Inventory.InventoryOf(player)?.Storages
            .Where(e => TradeGoods.IsTradeBag(e.Group))
            .SelectMany(e => e.Slots)
            .FirstOrDefault(e => e is not null && e.Serial == serial);
    }

    private async ValueTask FlushAsync(Entity buyer, Entity owner)
    {
        var first = persons.FlushAsync(buyer);
        var second = persons.FlushAsync(owner);

        await first;
        await second;
    }

    private void Refuse(Entity player, TradeOpcode request, ushort code)
    {
        Send(player, new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.Trade, request), code));
    }

    private void Send<T>(Entity player, T message) where T : IResponseHotspotMessage
    {
        if (world.IsAlive(player) && world.Has<ServerSessionComponent>(player))
        {
            world.Get<ServerSessionComponent>(player).Send(message);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Private shop of actor {Owner} sold item {Item} for {Price} gold to actor {Buyer}.")]
    private static partial void LogSold(ILogger logger, int owner, int item, long price, int buyer);
}
