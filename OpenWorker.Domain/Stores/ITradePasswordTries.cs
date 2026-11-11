namespace OpenWorker.Domain.Stores;

/// <summary>
/// The wrong trade passwords of an account and when the last one was given.
/// </summary>
/// <param name="Failed">The wrong passwords since the last right one.</param>
/// <param name="LastFailedAt">When the last wrong one was given (UTC), or nothing.</param>
public readonly record struct TradePasswordTries(int Failed, DateTime? LastFailedAt);

/// <summary>
/// Where the wrong tries at the trade password are kept: with the account, so every login of it, on every server, sees the
/// same count and the same lock. The count changes in one step in the database, so two servers never lose a try.
/// </summary>
public interface ITradePasswordTries
{
    /// <summary>
    /// The tries of the account (none for an account that does not exist).
    /// </summary>
    Task<TradePasswordTries> LoadAsync(int account, CancellationToken cancellationToken = default);

    /// <summary>
    /// One more wrong password at <paramref name="now"/>. The answer is the count after it.
    /// </summary>
    Task<int> FailAsync(int account, DateTime now, CancellationToken cancellationToken = default);

    /// <summary>
    /// The right password: the count goes back to zero.
    /// </summary>
    Task ClearAsync(int account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Takes the password of the account away and forgets the tries: the account has no trade password any more, as one that
    /// never made it. What the page of the account does in the original, for a locked password, and what a game master does here.
    /// </summary>
    Task ResetAsync(int account, CancellationToken cancellationToken = default);
}
