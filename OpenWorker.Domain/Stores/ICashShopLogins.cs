namespace OpenWorker.Domain.Stores;

/// <summary>
/// Whether the client of a login has been given the contents of the cash shop's window. The client keeps them through every
/// server of the login, so a district or a maze gives them only to a login that has not had them yet.
/// </summary>
public interface ICashShopLogins
{
    /// <summary>
    /// Whether the window of this login has been given its contents.
    /// </summary>
    Task<bool> IsGivenAsync(long session, CancellationToken cancellationToken = default);

    /// <summary>
    /// The window of this login has been given its contents.
    /// </summary>
    Task GiveAsync(long session, CancellationToken cancellationToken = default);
}
