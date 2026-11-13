using OpenWorker.Domain.Persistent;

namespace OpenWorker.Domain.Stores;

/// <summary>
/// One limited product of a purchase: how many more are bought and how many its period allows.
/// </summary>
/// <param name="ByAccount">The account counts, not the character that buys.</param>
/// <param name="Period">When the period of the count started (UTC).</param>
public readonly record struct CashLimitDraft(int Product, bool ByAccount, DateTime Period, int Count, int Limit);

/// <summary>
/// A purchase of the cash shop: who pays, how much, the counts it adds to and the letters that carry what was bought.
/// </summary>
public sealed record CashPurchaseDraft(
    int Account,
    int Buyer,
    long Price,
    IReadOnlyList<CashLimitDraft> Limits,
    IReadOnlyList<PostPersistent> Letters);

public enum CashPurchaseOutcome
{
    Done,

    /// <summary>
    /// The account has less than the price.
    /// </summary>
    NotEnough,

    /// <summary>
    /// A product would pass the count its period allows.
    /// </summary>
    Limit
}

/// <param name="Balance">What the account has after the purchase, or as it is when the purchase was refused.</param>
public readonly record struct CashPurchaseResult(CashPurchaseOutcome Outcome, long Balance);

/// <summary>
/// The character a present goes to.
/// </summary>
public readonly record struct CashReceiver(int Person, int Account, string Name, byte Hero);

/// <summary>
/// The cash shop, written at once: the balance of an account is shared by its characters on every server, so a purchase
/// takes it, counts the limited products and writes the letters with what was bought in one transaction. It never gives
/// without taking, nor takes without giving.
/// </summary>
public interface ICashShopStore
{
    /// <summary>
    /// The balance of the account, 0 for one that does not exist.
    /// </summary>
    Task<long> BalanceAsync(int account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an amount to the balance (takes it when below zero) in one statement, unless the balance would fall below zero or
    /// pass <paramref name="limit"/>. The balance after it, or nothing when it was refused or the account does not exist.
    /// </summary>
    Task<long?> GiveAsync(int account, long amount, long limit, CancellationToken cancellationToken = default);

    Task<CashPurchaseResult> BuyAsync(CashPurchaseDraft draft, CancellationToken cancellationToken = default);

    /// <summary>
    /// The character of that name, as any case of it.
    /// </summary>
    Task<CashReceiver?> FindPersonAsync(string name, CancellationToken cancellationToken = default);

    Task<List<CashShopSetPersistent>> ListSetsAsync(int person, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a list of the character over the one of that number.
    /// </summary>
    Task SaveSetAsync(CashShopSetPersistent list, CancellationToken cancellationToken = default);

    Task<bool> DeleteSetAsync(int person, byte setNo, CancellationToken cancellationToken = default);
}
