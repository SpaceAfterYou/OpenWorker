using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Persistent;

namespace OpenWorker.Persistence;

/// <summary>
/// What changed for one person since the last save: items to add, rewrite and remove, and the numbers kept on the person.
/// </summary>
public sealed class PersonChanges
{
    public required int Person { get; init; }
    public IReadOnlyList<ItemPersistent> Added { get; init; } = [];
    public IReadOnlyList<ItemPersistent> Changed { get; init; } = [];
    public IReadOnlyList<long> Removed { get; init; } = [];
    public PersonStateChanges? State { get; init; }

    public bool IsEmpty => Added.Count == 0 && Changed.Count == 0 && Removed.Count == 0 && State is null;
}

public sealed record PersonStateChanges(
    byte Level,
    int Experience,
    long Gold,
    long BattlePoint,
    long Ether,
    byte[] StorageGrades,
    int[] QuickSlotCards,
    int[] QuickSlotItems);

public static class ItemStore
{
    public static async Task<List<ItemPersistent>> LoadItemsAsync(
        this IDbContextFactory<PersistenceContext> factory,
        int person,
        CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        return await database.Items
            .AsNoTracking()
            .Where(e => e.PersonId == person)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Takes serials for new items. Several servers write to the same table, so the database hands them out.
    /// </summary>
    public static async Task<long[]> ReserveSerialsAsync(
        this IDbContextFactory<PersistenceContext> factory,
        int count,
        CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        var query = "SELECT nextval('\"" + PersistenceContext.ItemSerialSequence + "\"') AS \"Value\" FROM generate_series(1, {0})";

        var serials = await database.Database
            .SqlQueryRaw<long>(query, count)
            .ToArrayAsync(cancellationToken);

        return serials;
    }

    public static async Task SaveChangesAsync(
        this IDbContextFactory<PersistenceContext> factory,
        PersonChanges changes,
        CancellationToken cancellationToken = default)
    {
        if (changes.IsEmpty)
        {
            return;
        }

        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);

        if (changes.Removed.Count > 0)
        {
            var removed = changes.Removed.ToArray();

            await database.Items
                .Where(e => removed.Contains(e.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        foreach (var item in changes.Changed)
        {
            await database.Items
                .Where(e => e.Id == item.Id)
                .ExecuteUpdateAsync(
                    e => e
                        .SetProperty(x => x.Storage, item.Storage)
                        .SetProperty(x => x.Slot, item.Slot)
                        .SetProperty(x => x.Count, item.Count)
                        .SetProperty(x => x.BindType, item.BindType)
                        .SetProperty(x => x.Upgrade, item.Upgrade)
                        .SetProperty(x => x.Endurance, item.Endurance)
                        .SetProperty(x => x.SocketActive, item.SocketActive)
                        .SetProperty(x => x.DueTo, item.DueTo)
                        .SetProperty(x => x.UpgradeCount, item.UpgradeCount)
                        .SetProperty(x => x.UpgradeLimit, item.UpgradeLimit)
                        .SetProperty(x => x.Flag, item.Flag)
                        .SetProperty(x => x.Exp, item.Exp)
                        .SetProperty(x => x.BroachState, item.BroachState)
                        .SetProperty(x => x.RestoreCount, item.RestoreCount)
                        .SetProperty(x => x.SealCount, item.SealCount)
                        .SetProperty(x => x.OptionType, item.OptionType)
                        .SetProperty(x => x.OptionValue, item.OptionValue)
                        .SetProperty(x => x.UpdatedAt, DateTime.UtcNow),
                    cancellationToken);
        }

        if (changes.Added.Count > 0)
        {
            database.Items.AddRange(changes.Added);

            await database.SaveChangesAsync(cancellationToken);
        }

        if (changes.State is { } state)
        {
            await database.Persons
                .Where(e => e.Id == changes.Person)
                .ExecuteUpdateAsync(
                    e => e
                        .SetProperty(x => x.Level, state.Level)
                        .SetProperty(x => x.Experience, state.Experience)
                        .SetProperty(x => x.Gold, state.Gold)
                        .SetProperty(x => x.BattlePoint, state.BattlePoint)
                        .SetProperty(x => x.Ether, state.Ether)
                        .SetProperty(x => x.StorageGrades, state.StorageGrades)
                        .SetProperty(x => x.QuickSlotCards, state.QuickSlotCards)
                        .SetProperty(x => x.QuickSlotItems, state.QuickSlotItems),
                    cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
