using OpenWorker.Domain.Persistent;

namespace OpenWorker.Domain.Stores;

/// <summary>
/// Some of an item a person gives to the exchange to wait there, and what the search will filter it by.
/// </summary>
public sealed record ExchangeListingDraft(
    int Seller,
    string SellerName,
    byte SellerHero,
    PostItemPersistent Goods,
    byte Group,
    byte SubGroup,
    byte UseClass,
    short Level,
    byte Rank);

/// <summary>
/// What the search window asks for; a filter below zero is not set.
/// </summary>
public sealed record ExchangeSearch(
    int Item,
    int Group,
    int SubGroup,
    int UseClass,
    int LevelMin,
    int LevelMax,
    int RankMin,
    int RankMax,
    long PriceMax,
    int UpgradeMin);

public enum ExchangeOutcome
{
    Done,

    /// <summary>
    /// No such listing, or not of the one who asks: sold out or taken back.
    /// </summary>
    Missing,

    /// <summary>
    /// The person has as many listings of that state as it may.
    /// </summary>
    Full,

    /// <summary>
    /// The listing is not in the state the request needs.
    /// </summary>
    WrongState,

    /// <summary>
    /// The sale has ended.
    /// </summary>
    Expired,

    /// <summary>
    /// Fewer are left than asked.
    /// </summary>
    NotEnough,

    /// <summary>
    /// The buyer sells it.
    /// </summary>
    Own,

    /// <summary>
    /// The listing is not what the buyer was shown: another item or another price.
    /// </summary>
    Changed
}

public enum ExchangeInterestOutcome
{
    Added,
    Already,
    Full
}

/// <param name="Listing">The listing as it is now: after the change, or as it was when it went.</param>
public readonly record struct ExchangeListingResult(ExchangeOutcome Outcome, ExchangeListingPersistent? Listing);

/// <param name="Listing">The listing after the sale; its count is 0 when the sale took the last of it, and then it is gone.</param>
/// <param name="ToBuyer">The letter with what was bought.</param>
/// <param name="ToSeller">The letter with the gold of the sale.</param>
public readonly record struct ExchangeBuyResult(
    ExchangeOutcome Outcome,
    ExchangeListingPersistent? Listing,
    PostPersistent? ToBuyer,
    PostPersistent? ToSeller);

/// <param name="Listing">The listing that was taken back.</param>
/// <param name="Letter">The letter with what was left of it.</param>
public readonly record struct ExchangeRecallResult(ExchangeOutcome Outcome, ExchangeListingPersistent? Listing, PostPersistent? Letter);

/// <summary>
/// The letters a sale writes: to the buyer with what was bought, to the seller with the gold.
/// </summary>
public delegate (PostPersistent ToBuyer, PostPersistent ToSeller) ExchangeSaleLetters(ExchangeListingPersistent listing, short count);

/// <summary>
/// The exchange, shared by every server: a listing is written by this store when it changes, at once, and every change
/// that two servers could make at the same moment is one statement or one transaction that holds the rows it counts.
/// What moves to a person moves by letter, written in the same transaction as the change of the listing.
/// </summary>
public interface IExchangeStore
{
    Task<List<int>> ListInterestsAsync(int person, CancellationToken cancellationToken = default);

    /// <summary>
    /// Puts an item on the list of a person, unless it is there or the list holds <paramref name="limit"/> items.
    /// </summary>
    Task<ExchangeInterestOutcome> AddInterestAsync(int person, int item, int limit, CancellationToken cancellationToken = default);

    Task<bool> RemoveInterestAsync(int person, int item, CancellationToken cancellationToken = default);

    /// <summary>
    /// The listings of a seller, the oldest first.
    /// </summary>
    Task<List<ExchangeListingPersistent>> ListMineAsync(int person, CancellationToken cancellationToken = default);

    Task<ExchangeListingPersistent?> FindAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores the item to wait, unless the seller already has <paramref name="limit"/> waiting listings.
    /// </summary>
    Task<ExchangeListingResult> AddWaitingAsync(ExchangeListingDraft draft, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Puts a waiting listing of the seller on sale, unless the seller already has <paramref name="limit"/> listings on sale.
    /// </summary>
    Task<ExchangeListingResult> PutOnSaleAsync(
        long serial,
        int seller,
        long priceOne,
        byte saleRate,
        DateTime expiresAt,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Takes a listing of the seller back, whatever its state, and writes the letter that returns what is left of it.
    /// </summary>
    Task<ExchangeRecallResult> RecallAsync(
        long serial,
        int seller,
        Func<ExchangeListingPersistent, PostPersistent> letter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// A page of the listings on sale that match, the cheapest first, and how many match.
    /// </summary>
    Task<(int Total, List<ExchangeListingPersistent> Page)> SearchAsync(
        ExchangeSearch search,
        int page,
        int pageSize,
        DateTime now,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sells some of a listing: one statement takes the count off it only while it is on sale, has not ended, holds that many,
    /// is not the buyer's own and still has the price the buyer was shown, so two buyers never get the same item. The sale,
    /// its letters and the end of a listing sold out go in the same transaction.
    /// </summary>
    Task<ExchangeBuyResult> BuyAsync(
        int id,
        int buyer,
        int item,
        short count,
        long priceOne,
        DateTime now,
        ExchangeSaleLetters letters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The last sales of an item, the newest first.
    /// </summary>
    Task<List<ExchangeSalePersistent>> ListSalesAsync(int item, int count, CancellationToken cancellationToken = default);
}
