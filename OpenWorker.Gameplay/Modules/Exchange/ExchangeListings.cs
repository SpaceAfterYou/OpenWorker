using System.Globalization;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Post;
using OpenWorker.Hotspot.Modules.Exchange.Types;
using OpenWorker.Hotspot.Modules.Items.Types;

namespace OpenWorker.Gameplay.Modules.Exchange;

/// <summary>
/// Listings as the client sees them, and the letters that carry what a listing gives back.
/// </summary>
public static class ExchangeListings
{
    /// <summary>
    /// Who signs the letters of the exchange.
    /// </summary>
    public const string Sender = "Exchange";

    #region As the client sees them

    /// <summary>
    /// A listing in the list of its seller.
    /// </summary>
    public static ExchangeMyItemValue Mine(ExchangeListingPersistent listing)
    {
        return new ExchangeMyItemValue
        {
            Exchange = listing.Id,
            ExpiresAt = UnixOf(listing.ExpiresAt),
            State = (byte)listing.State,
            InitialCount = listing.InitialCount,
            PriceOne = listing.PriceOne,
            Goods = Goods(listing, listing.Count)
        };
    }

    /// <summary>
    /// A listing as a search shows it to a buyer.
    /// </summary>
    public static ExchangeItemValue Found(ExchangeListingPersistent listing)
    {
        return new ExchangeItemValue
        {
            Exchange = listing.Id,
            PriceOne = listing.PriceOne,
            ExpiresAt = UnixOf(listing.ExpiresAt),
            NoCommission = (byte)(listing.SaleRate == 0 ? 1 : 0),
            Goods = Goods(listing, listing.Count)
        };
    }

    public static ExchangePriceValue Sale(ExchangeSalePersistent sale)
    {
        return new ExchangePriceValue
        {
            Item = sale.Item,
            Count = sale.Count,
            PriceOne = sale.PriceOne,
            SoldAt = UnixOf(sale.SoldAt)
        };
    }

    /// <summary>
    /// The item of a listing under the serial of the listing, with its gems and broaches.
    /// </summary>
    public static ExchangeGoodsValue Goods(ExchangeListingPersistent listing, short count)
    {
        var goods = listing.Goods;

        var item = ItemInstance.From(new ItemPersistent
        {
            Id = listing.Serial,
            Item = goods.Item,
            Count = count,
            BindType = goods.BindType,
            Upgrade = goods.Upgrade,
            Endurance = goods.Endurance,
            SocketActive = goods.SocketActive,
            DueTo = goods.DueTo,
            UpgradeCount = goods.UpgradeCount,
            UpgradeLimit = goods.UpgradeLimit,
            Flag = goods.Flag,
            Exp = goods.Exp,
            BroachState = goods.BroachState,
            RestoreCount = goods.RestoreCount,
            SealCount = goods.SealCount,
            OptionType = goods.OptionType,
            OptionValue = goods.OptionValue,
            Sockets = goods.Sockets ?? [],
            SocketOptionType = goods.SocketOptionType ?? [],
            SocketOptionValue = goods.SocketOptionValue ?? [],
            Broaches = goods.Broaches ?? []
        });

        var broach = item.BroachValue();

        return new ExchangeGoodsValue
        {
            Item = item.ToValue(),
            Sockets = item.SocketValues()
                .Select(e => new ItemSocketValue { EquipSerial = e.biEquipSerial.Number, Socket = e.dwSocketID, Position = e.bySocketPos, Options = e.stExtendOption })
                .ToArray(),
            Broach = new ItemBroachValue
            {
                Serial = listing.Serial,
                Items = broach?.dwItemID ?? new int[ItemBroachValue.ItemCount]
            }
        };
    }

    private static long UnixOf(DateTime? time)
    {
        return time is { } value ? new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)).ToUnixTimeSeconds() : 0;
    }

    #endregion

    #region Letters

    /// <summary>
    /// The letters of a sale: the item to the buyer, the gold less the share of the exchange to the seller. The client fills
    /// the text of the row with the item named by the title and the count written as the text.
    /// </summary>
    public static (PostPersistent ToBuyer, PostPersistent ToSeller) SaleLetters(
        ExchangeListingPersistent listing,
        short count,
        int buyer,
        string buyerName,
        byte buyerHero,
        DateTime now)
    {
        var gets = ExchangeRules.SellerGets(listing.PriceOne * count, listing.SaleRate);
        var item = listing.Item.ToString(CultureInfo.InvariantCulture);
        var sold = count.ToString(CultureInfo.InvariantCulture);

        var toBuyer = Letter(buyer, buyerName, buyerHero, GameLetters.ExchangeBought, item, sold, 0, Part(listing, count), now);

        var toSeller = Letter(listing.SellerId, listing.SellerName, listing.SellerHero,
            listing.SaleRate == 0 ? GameLetters.ExchangeSoldWithoutCommission : GameLetters.ExchangeSold, item, sold, gets, null, now);

        return (toBuyer, toSeller);
    }

    /// <summary>
    /// The letter that returns what is left of a listing taken back: before its end, or after it. The row says it all.
    /// </summary>
    public static PostPersistent ReturnLetter(ExchangeListingPersistent listing, DateTime now)
    {
        var ended = listing.State == ExchangeListingState.OnSale && listing.ExpiresAt <= now;

        return Letter(listing.SellerId, listing.SellerName, listing.SellerHero, ended ? GameLetters.ExchangeEnded : GameLetters.ExchangeTakenBack, string.Empty, string.Empty,
            0, Part(listing, listing.Count), now);
    }

    /// <summary>
    /// Some of the item of a listing, with everything that was done to it.
    /// </summary>
    private static PostItemPersistent Part(ExchangeListingPersistent listing, short count)
    {
        var goods = listing.Goods;

        return new PostItemPersistent
        {
            Item = goods.Item,
            Count = count,
            BindType = goods.BindType,
            Upgrade = goods.Upgrade,
            Endurance = goods.Endurance,
            SocketActive = goods.SocketActive,
            DueTo = goods.DueTo,
            UpgradeCount = goods.UpgradeCount,
            UpgradeLimit = goods.UpgradeLimit,
            Flag = goods.Flag,
            Exp = goods.Exp,
            BroachState = goods.BroachState,
            RestoreCount = goods.RestoreCount,
            SealCount = goods.SealCount,
            OptionType = goods.OptionType,
            OptionValue = goods.OptionValue,
            Sockets = goods.Sockets,
            SocketOptionType = goods.SocketOptionType,
            SocketOptionValue = goods.SocketOptionValue,
            Broaches = goods.Broaches
        };
    }

    private static PostPersistent Letter(
        int receiver,
        string receiverName,
        byte receiverHero,
        byte kind,
        string title,
        string body,
        long money,
        PostItemPersistent? item,
        DateTime now)
    {
        return new PostPersistent
        {
            Kind = PostKind.Person,
            SenderName = Sender,
            ReceiverId = receiver,
            ReceiverName = receiverName,
            ReceiverHero = receiverHero,
            Title = title,
            Body = body,
            Money = money,
            Items = item is null ? [] : [item],
            MainType = GameLetters.PersonKind,
            SubType = kind,
            State = PostState.None,
            CreatedAt = now,
            UpdatedAt = now,
            ExpiresAt = now + PostLimits.Lifetime
        };
    }

    #endregion
}
