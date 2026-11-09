using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Stores;

namespace OpenWorker.Persistence;

/// <summary>
/// The prologue flag of a character over the database: one query for each question, so the answer is what the row holds now.
/// </summary>
public sealed class PrologueStore(IDbContextFactory<PersistenceContext> factory) : IPrologueStore
{
    public async Task<bool> IsPendingAsync(int person, int account, short gate, CancellationToken cancellationToken = default)
    {
        await using var database = await factory
            .CreateDbContextAsync(cancellationToken);

        return await database.Persons
            .AnyAsync(e => e.Id == person && e.Account.Id == account && e.Gate.Id == gate && e.ProloguePending, cancellationToken);
    }

    public async Task<bool> FinishAsync(int person, CancellationToken cancellationToken = default)
    {
        await using var database = await factory
            .CreateDbContextAsync(cancellationToken);

        // The condition makes the change once: a second call, from this server or another, finds nothing to change.
        var changed = await database.Persons
            .Where(e => e.Id == person && e.ProloguePending)
            .ExecuteUpdateAsync(e => e.SetProperty(p => p.ProloguePending, false), cancellationToken);

        return changed > 0;
    }
}
