namespace OpenWorker.DistrictServer.Trade;

/// <summary>
/// The messages the client shows for a trade or a private shop that does not go through, sent as the cause of a
/// cancel or as the code of an error answer.
/// </summary>
public static class TradeCodes
{
    /// <summary>The other player is in a trade or runs a shop.</summary>
    public const ushort TargetBusy = 52401;

    /// <summary>The other player is not in the channel any more.</summary>
    public const ushort TargetAway = 52402;

    /// <summary>The request does not fit the state of the trade.</summary>
    public const ushort Refused = 52403;

    /// <summary>The player has less gold than it offers or pays.</summary>
    public const ushort NotEnoughGold = 52404;

    /// <summary>The item is on the table already.</summary>
    public const ushort AlreadyOffered = 52405;

    /// <summary>Five items are on the table already; also the cause when the items cannot be gathered at the exchange.</summary>
    public const ushort ItemsUnavailable = 52406;

    /// <summary>The slot holds nothing that can be traded.</summary>
    public const ushort ItemRefused = 52407;

    /// <summary>The player is in a trade or runs a shop.</summary>
    public const ushort Busy = 52408;

    /// <summary>Nobody of that name is on this server.</summary>
    public const ushort NameUnknown = 52409;

    /// <summary>The other player has logged out.</summary>
    public const ushort LoggedOut = 52410;

    /// <summary>The invitation was not answered in time.</summary>
    public const ushort Expired = 52412;

    /// <summary>The player has blocked the other.</summary>
    public const ushort Blocked = 52416;

    /// <summary>The gold would not fit the purse of the other player.</summary>
    public const ushort TooMuchGold = 52417;

    /// <summary>The item is bound to its owner.</summary>
    public const ushort ItemBound = 55602;

    /// <summary>The shop sells: stop it before changing it, or it is open already.</summary>
    public const ushort ShopSelling = 52430;

    /// <summary>The item or the price cannot go into the shop.</summary>
    public const ushort ShopItemRefused = 52431;

    /// <summary>The shop is full, or the line is not in it.</summary>
    public const ushort ShopFull = 52432;

    /// <summary>There is no shop, or nothing in it.</summary>
    public const ushort NoShop = 52434;

    /// <summary>The shop does not sell right now.</summary>
    public const ushort ShopNotSelling = 52435;

    /// <summary>The owner of the shop is not here.</summary>
    public const ushort ShopGone = 52436;
}

/// <summary>
/// What the client reads in the result of an exchange: it shows a message for <see cref="Done"/> and <see cref="Failed"/>
/// and closes the window in every case.
/// </summary>
public enum TradeResult : byte
{
    Done = 0,

    /// <summary>The items do not fit the bags.</summary>
    Failed = 1,

    /// <summary>A side no longer has the gold it offered.</summary>
    NoGold = 2
}
