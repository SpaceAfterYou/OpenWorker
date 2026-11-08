using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Persistent;

namespace OpenWorker.Persistence;

/// <summary>
/// A person as a district or a maze loads it when the person enters, with the options of its account.
/// </summary>
public sealed record PersonAtEntry(PersonPersistent Person, byte[] AccountOptions);

/// <summary>
/// The queries every entry runs, compiled once: EF Core does not translate them again for each player.
/// </summary>
public static class PersonQueries
{
    private static readonly Func<PersistenceContext, int, int, CancellationToken, Task<PersonAtEntry?>> AtEntry =
        EF.CompileAsyncQuery((PersistenceContext database, int account, int person, CancellationToken _) => database.Persons
            .AsNoTracking()
            .Where(e => e.Id == person && e.Account.Id == account)
            .Select(e => new PersonAtEntry(e, e.Account.OptionList))
            .FirstOrDefault());

    private static readonly Func<PersistenceContext, string, CancellationToken, Task<AccountPersistent?>> AccountByName =
        EF.CompileAsyncQuery((PersistenceContext database, string username, CancellationToken _) => database.Accounts
            .AsNoTracking()
            .FirstOrDefault(e => e.Username == username));

    /// <summary>
    /// The person of the account, or null when the account has no such person.
    /// </summary>
    public static async Task<PersonAtEntry?> LoadPersonAtEntryAsync(
        this IDbContextFactory<PersistenceContext> factory,
        int account,
        int person,
        CancellationToken cancellationToken)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        return await AtEntry(database, account, person, cancellationToken);
    }

    /// <summary>
    /// The account a login names.
    /// </summary>
    public static async Task<AccountPersistent?> FindAccountAsync(
        this IDbContextFactory<PersistenceContext> factory,
        string username,
        CancellationToken cancellationToken)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        return await AccountByName(database, username, cancellationToken);
    }
}
