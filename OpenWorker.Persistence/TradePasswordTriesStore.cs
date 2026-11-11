using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Stores;

namespace OpenWorker.Persistence;

/// <summary>
/// The wrong trade passwords of an account, kept in its row. Every change is one statement, so two servers that count a
/// wrong password for one account at once both count it.
/// </summary>
public sealed class TradePasswordTriesStore(IDbContextFactory<PersistenceContext> factory) : ITradePasswordTries
{
    public async Task<TradePasswordTries> LoadAsync(int account, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        var found = await database.Accounts
            .AsNoTracking()
            .Where(e => e.Id == account)
            .Select(e => new { e.TradePasswordFailed, e.TradePasswordFailedAt })
            .FirstOrDefaultAsync(cancellationToken);

        return found is null
            ? default
            : new TradePasswordTries(found.TradePasswordFailed, found.TradePasswordFailedAt is { } at ? DateTime.SpecifyKind(at, DateTimeKind.Utc) : null);
    }

    public async Task<int> FailAsync(int account, DateTime now, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        var counts = await database.Database
            .SqlQuery<short>($"""
                UPDATE "Accounts" SET "TradePasswordFailed" = "TradePasswordFailed" + 1, "TradePasswordFailedAt" = {now}
                WHERE "Id" = {account}
                RETURNING "TradePasswordFailed" AS "Value"
                """)
            .ToArrayAsync(cancellationToken);

        return counts.Length == 0 ? 0 : counts[0];
    }

    public async Task ClearAsync(int account, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        await database.Accounts
            .Where(e => e.Id == account && e.TradePasswordFailed != 0)
            .ExecuteUpdateAsync(
                e => e.SetProperty(x => x.TradePasswordFailed, (short)0).SetProperty(x => x.TradePasswordFailedAt, (DateTime?)null),
                cancellationToken);
    }

    public async Task ResetAsync(int account, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        await database.Accounts
            .Where(e => e.Id == account)
            .ExecuteUpdateAsync(
                e => e.SetProperty(x => x.TradePassword, string.Empty)
                    .SetProperty(x => x.TradePasswordFailed, (short)0)
                    .SetProperty(x => x.TradePasswordFailedAt, (DateTime?)null),
                cancellationToken);
    }
}
