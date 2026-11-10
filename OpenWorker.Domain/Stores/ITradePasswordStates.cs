namespace OpenWorker.Domain.Stores;

/// <summary>
/// Where a player stands with the trade password of its account: given on this login, or locked by wrong answers. A player
/// who has done neither has no record.
/// </summary>
/// <param name="Locked">Too many wrong answers: nothing is accepted any more.</param>
/// <param name="Authenticated">The right password was given on this login.</param>
/// <param name="Failed">The wrong answers since the right one.</param>
public readonly record struct TradePasswordRecord(bool Authenticated, bool Locked, int Failed);

/// <summary>
/// What the servers of one login share about the trade password: the player gives it in the district, enters a maze and
/// finds it still given, or locked, there and in the district again. The record belongs to the login (the session of the
/// lease), so it ends with the login: a lease that nobody holds any more takes it along.
/// </summary>
public interface ITradePasswordStates
{
    /// <summary>
    /// The record of the session, or nothing for a session that has given neither a right nor a wrong answer.
    /// </summary>
    Task<TradePasswordRecord?> LoadAsync(long session, CancellationToken cancellationToken = default);

    /// <summary>
    /// The right password was given (or just created): the record says so and forgets the wrong answers. A locked record
    /// stays locked, and the answer is the record as it stands.
    /// </summary>
    Task<TradePasswordRecord> AuthenticateAsync(long session, CancellationToken cancellationToken = default);

    /// <summary>
    /// A wrong password was given. The count of wrong answers goes up in one step, so two servers cannot both miss the
    /// answer that locks it, and reaching <paramref name="limit"/> locks the record. The answer is the record after it.
    /// </summary>
    Task<TradePasswordRecord> FailAsync(long session, int limit, CancellationToken cancellationToken = default);
}
