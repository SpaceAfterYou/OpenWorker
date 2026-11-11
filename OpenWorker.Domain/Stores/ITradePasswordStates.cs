namespace OpenWorker.Domain.Stores;

/// <summary>
/// Whether the player has given the trade password on this login. A login is the lease of a session, and the servers one
/// player goes through (a district, a maze, a district again) are all of that login: the password given on one is given on
/// the next, so the player is not asked again at every move. The wrong tries are not kept here: they belong to the account
/// (<see cref="ITradePasswordTries"/>).
/// </summary>
public interface ITradePasswordStates
{
    /// <summary>
    /// Whether the right password has been given on this login and not taken back since.
    /// </summary>
    Task<bool> IsGivenAsync(long session, CancellationToken cancellationToken = default);

    /// <summary>
    /// The right password was given (or just made).
    /// </summary>
    Task GiveAsync(long session, CancellationToken cancellationToken = default);

    /// <summary>
    /// A wrong password was given: the one given before is taken back.
    /// </summary>
    Task TakeBackAsync(long session, CancellationToken cancellationToken = default);
}
