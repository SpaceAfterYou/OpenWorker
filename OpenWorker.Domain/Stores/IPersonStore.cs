using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;

namespace OpenWorker.Domain.Stores;

/// <summary>
/// The rows of a person that a game server reads when the person enters and writes back by its flush: the items, the
/// serials of new items, the progress documents of its features, and the changes of <see cref="PersonChanges"/>.
/// The game logic sees this interface only; <c>OpenWorker.Persistence</c> implements it over the database, so the game
/// knows nothing of EF Core.
/// </summary>
public interface IPersonStore
{
    /// <summary>
    /// Every item of the person, as stored.
    /// </summary>
    Task<List<ItemPersistent>> LoadItemsAsync(int person, CancellationToken cancellationToken = default);

    /// <summary>
    /// Takes <paramref name="count"/> serials for new items. Several servers write to the same table, so the database hands
    /// them out.
    /// </summary>
    Task<long[]> ReserveSerialsAsync(int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// The progress document of one feature of the person, serialized, or <see langword="null"/> when there is none yet.
    /// </summary>
    Task<string?> LoadDocumentAsync(int person, PersonStateKind kind, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every progress document the person has, serialized, by kind, in one query: a kind without a document is not in it.
    /// </summary>
    Task<Dictionary<PersonStateKind, string>> LoadDocumentsAsync(int person, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes the changes of many persons in one transaction, in the order given: the changes of one person that appear
    /// twice are written oldest first.
    /// </summary>
    Task SaveChangesAsync(IReadOnlyList<PersonChanges> batch, CancellationToken cancellationToken = default);
}

public static class PersonStoreExtensions
{
    /// <summary>
    /// Writes the changes of one person.
    /// </summary>
    public static Task SaveChangesAsync(this IPersonStore store, PersonChanges changes, CancellationToken cancellationToken = default)
    {
        return store.SaveChangesAsync([changes], cancellationToken);
    }
}
