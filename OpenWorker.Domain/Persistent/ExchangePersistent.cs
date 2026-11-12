namespace OpenWorker.Domain.Persistent;

/// <summary>
/// Where a listing of the exchange stands. An expired listing stays on sale in the list of its seller until it is taken
/// back; the buyers no longer see it.
/// </summary>
public enum ExchangeListingState : byte
{
    /// <summary>
    /// Given to the exchange, not on sale yet: it has no price and no end.
    /// </summary>
    Waiting = 1,

    OnSale = 2
}

/// <summary>
/// Some of an item a person gave to the exchange. The item left the bags of the seller when it was given: it lives here,
/// with everything that was done to it, until it is sold, taken back or bought out, and goes on by letter.
/// </summary>
public sealed class ExchangeListingPersistent : BasicPersistent
{
    public const int MaxNameLength = PostPersistent.MaxNameLength;

    /// <summary>
    /// The number of the listing, as the buyers name it.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// The serial the list of the seller keeps the listing by, from the sequence of the item serials.
    /// </summary>
    public long Serial { get; init; }

    public int SellerId { get; init; }

    public required string SellerName { get; init; }

    public byte SellerHero { get; init; }

    public int Item { get; init; }

    /// <summary>
    /// The item as it was given, with everything done to it; its count is the count given.
    /// </summary>
    public required PostItemPersistent Goods { get; init; }

    public short InitialCount { get; init; }

    /// <summary>
    /// How many are left.
    /// </summary>
    public short Count { get; set; }

    public ExchangeListingState State { get; set; }

    /// <summary>
    /// The price of one, 0 while the listing waits.
    /// </summary>
    public long PriceOne { get; set; }

    /// <summary>
    /// The share of a sale the exchange keeps, in percent.
    /// </summary>
    public byte SaleRate { get; set; }

    /// <summary>
    /// When the sale ends; none while the listing waits.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    // What the search filters by, taken from the tables when the item was given.
    public byte Group { get; init; }
    public byte SubGroup { get; init; }
    public byte UseClass { get; init; }
    public short Level { get; init; }
    public byte Rank { get; init; }
    public byte Upgrade { get; init; }
}

/// <summary>
/// An item a person keeps an eye on at the exchange.
/// </summary>
public sealed class ExchangeInterestPersistent
{
    public int PersonId { get; init; }

    public int Item { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// A sale of the exchange, kept for the prices the search window shows.
/// </summary>
public sealed class ExchangeSalePersistent
{
    public long Id { get; init; }

    public int Item { get; init; }

    public short Count { get; init; }

    public long PriceOne { get; init; }

    public DateTime SoldAt { get; init; }
}
