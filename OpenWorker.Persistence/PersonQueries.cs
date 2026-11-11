using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using OpenWorker.Domain.Persistent;

namespace OpenWorker.Persistence;

/// <summary>
/// A person as a district or a maze loads it when the person enters, with what its account keeps for every person: the
/// options, the access level and the hash of the trade password.
/// </summary>
public sealed record PersonAtEntry(PersonPersistent Person, byte[] AccountOptions, byte AccessLevel, string TradePassword, short TradePasswordFailed);

/// <summary>
/// The queries every entry runs, compiled once: EF Core does not translate them again for each player.
/// </summary>
public static class PersonQueries
{
    private static readonly Compiled<Func<PersistenceContext, int, int, CancellationToken, Task<PersonAtEntry?>>> AtEntry = new(() =>
        EF.CompileAsyncQuery((PersistenceContext database, int account, int person, CancellationToken _) => database.Persons
            .AsNoTracking()
            .Where(e => e.Id == person && e.Account.Id == account)
            .Select(e => new PersonAtEntry(e, e.Account.OptionList, e.Account.AccessLevel, e.Account.TradePassword, e.Account.TradePasswordFailed))
            .FirstOrDefault()));

    private static readonly Compiled<Func<PersistenceContext, string, CancellationToken, Task<AccountPersistent?>>> AccountByName = new(() =>
        EF.CompileAsyncQuery((PersistenceContext database, string username, CancellationToken _) => database.Accounts
            .AsNoTracking()
            .FirstOrDefault(e => e.Username == username)));

    private static readonly Compiled<Func<PersistenceContext, int, short, byte, CancellationToken, Task<AccountPersistent?>>> AccountAtGate = new(() =>
        EF.CompileAsyncQuery((PersistenceContext database, int account, short gate, byte lastStorage, CancellationToken _) => database.Accounts
            .AsNoTracking()
            .Include(e => e.Persons.Where(p => p.Gate.Id == gate))
            .ThenInclude(p => p.Items.Where(i => i.Storage <= lastStorage))
            .FirstOrDefault(e => e.Id == account)));

    /// <summary>
    /// The account as the gate shows it: its persons of the gate, with the items of the storages up to
    /// <paramref name="lastStorage"/> (what they wear).
    /// </summary>
    public static async Task<AccountPersistent?> LoadAccountAtGateAsync(
        this IDbContextFactory<PersistenceContext> factory,
        int account,
        short gate,
        byte lastStorage,
        CancellationToken cancellationToken)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        return await AccountAtGate.For(database)(database, account, gate, lastStorage, cancellationToken);
    }

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

        return await AtEntry.For(database)(database, account, person, cancellationToken);
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

        return await AccountByName.For(database)(database, username, cancellationToken);
    }

    /// <summary>
    /// A compiled query is bound to the model it first ran with. A server has one model, a test host makes one per
    /// container, so each model gets its own compilation.
    /// </summary>
    private sealed class Compiled<TQuery>(Func<TQuery> compile) where TQuery : class
    {
        private readonly ConditionalWeakTable<IModel, TQuery> _queries = new();

        public TQuery For(DbContext database) => _queries.GetValue(database.Model, _ => compile());
    }
}
