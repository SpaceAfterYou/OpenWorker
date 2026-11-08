using Microsoft.EntityFrameworkCore;

namespace OpenWorker.Persistence;

/// <summary>
/// The options of the client (the system menu) belong to the account: the client treats them as one set for all its
/// persons. Each is one statement by the key of the account.
/// </summary>
public static class PersonOptionStore
{
    public static async Task<byte[]> LoadPersonOptionsAsync(
        this IDbContextFactory<PersistenceContext> factory,
        int account,
        CancellationToken cancellationToken)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        var options = await database.Accounts
            .Where(e => e.Id == account)
            .Select(e => e.OptionList)
            .FirstOrDefaultAsync(cancellationToken);

        return options ?? [];
    }

    public static async Task SavePersonOptionsAsync(
        this IDbContextFactory<PersistenceContext> factory,
        int account,
        byte[] options,
        CancellationToken cancellationToken)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        await database.Accounts
            .Where(e => e.Id == account)
            .ExecuteUpdateAsync(e => e.SetProperty(x => x.OptionList, options), cancellationToken);
    }
}
