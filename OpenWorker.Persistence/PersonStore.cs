using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;

namespace OpenWorker.Persistence;

/// <summary>
/// The rows of a person over the database, for the game logic: each call opens a context of the pool and closes it.
/// </summary>
public sealed class PersonStore(IDbContextFactory<PersistenceContext> factory) : IPersonStore
{
    public Task<List<ItemPersistent>> LoadItemsAsync(int person, CancellationToken cancellationToken = default)
    {
        return factory.LoadItemsAsync(person, cancellationToken);
    }

    public Task<long[]> ReserveSerialsAsync(int count, CancellationToken cancellationToken = default)
    {
        return factory.ReserveSerialsAsync(count, cancellationToken);
    }

    public async Task<string?> LoadDocumentAsync(int person, PersonStateKind kind, CancellationToken cancellationToken = default)
    {
        await using var database = await factory
            .CreateDbContextAsync(cancellationToken);

        return await database.PersonStates
            .Where(e => e.PersonId == person && e.Kind == kind)
            .Select(e => e.Payload)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task SaveChangesAsync(IReadOnlyList<PersonChanges> batch, CancellationToken cancellationToken = default)
    {
        return factory.SaveChangesAsync(batch, cancellationToken);
    }
}
