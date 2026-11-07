using Microsoft.EntityFrameworkCore;

namespace OpenWorker.Persistence;

/// <summary>
/// The client treats these options as account-wide, while they are stored on every person.
/// </summary>
public static class PersonOptionStore
{
    public static async Task<byte[]> LoadPersonOptionsAsync(
        this IDbContextFactory<PersistenceContext> factory,
        int account,
        CancellationToken cancellationToken)
    {
        await using var database = await factory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        var options = await database.Persons
            .AsNoTracking()
            .Where(e => e.Account.Id == account)
            .Select(e => e.OptionList)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return options ?? [];
    }

    public static async Task SavePersonOptionsAsync(
        this IDbContextFactory<PersistenceContext> factory,
        int account,
        byte[] options,
        CancellationToken cancellationToken)
    {
        await using var database = await factory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        var persons = await database.Persons
            .Where(e => e.Account.Id == account)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var person in persons)
        {
            person.OptionList = options;
        }

        await database
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
