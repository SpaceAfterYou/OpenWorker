using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;

namespace OpenWorker.Persistence;

/// <summary>
/// The rankings: who cleared a map the fastest, and who has the most kills.
/// </summary>
public static class RankingStore
{
    public const int PageSize = 50;

    /// <summary>
    /// Whether the ranking is of the fastest clear (a time attack, a stage of the tower) rather than of the highest score.
    /// </summary>
    public static bool IsTimed(RankingKind kind) => kind is RankingKind.TimeAttack or RankingKind.InfiniteTower;

    /// <summary>
    /// Records a result for the person, keeping the better of it and the old one. A faster clear is better in a time
    /// attack (and with the same time, a higher score); a higher score is better in the arena. True when it was written.
    /// </summary>
    public static async Task<bool> SubmitRankingAsync(this IDbContextFactory<PersistenceContext> factory, RankingKind kind, int scope, int person, int time, int score)
    {
        await using var database = await factory.CreateDbContextAsync();

        var entry = await database.RankingEntries.AsTracking().FirstOrDefaultAsync(e => e.Kind == kind && e.Scope == scope && e.PersonId == person);

        if (entry is null)
        {
            database.RankingEntries.Add(new RankingEntryPersistent { Kind = kind, Scope = scope, PersonId = person, Time = time, Score = score });
        }

        else if (IsTimed(kind)
                     ? time < entry.Time || (time == entry.Time && score > entry.Score)
                     : score > entry.Score)
        {
            entry.Time = time;
            entry.Score = score;
        }

        else
        {
            return false;
        }

        try
        {
            await database.SaveChangesAsync();

            return true;
        }

        catch (DbUpdateException)
        {
            // Two results of the same person at once, the other one stays.
            return false;
        }
    }

    /// <summary>
    /// The best of a ranking, in order, and the place of the person asking. A person who is not in it is given no place.
    /// </summary>
    public static async Task<(List<RankingRow> Top, RankingRow? Mine)> RankingAsync(
        this IDbContextFactory<PersistenceContext> factory,
        RankingKind kind,
        int scope,
        int person)
    {
        await using var database = await factory.CreateDbContextAsync();

        var query = database.RankingEntries
            .AsNoTracking()
            .Where(e => e.Kind == kind && e.Scope == scope)
            .Join(database.Persons.AsNoTracking(), e => e.PersonId, e => e.Id, (entry, person) => new { entry, person });

        var ordered = IsTimed(kind)
            ? query.OrderBy(e => e.entry.Time).ThenByDescending(e => e.entry.Score).ThenBy(e => e.entry.Id)
            : query.OrderByDescending(e => e.entry.Score).ThenBy(e => e.entry.Id);

        var rows = await ordered
            .Select(e => new { e.person.Id, e.person.Name, e.person.Hero, e.person.Level, e.entry.Time, e.entry.Score })
            .Take(PageSize)
            .ToListAsync();

        var top = rows.Select((e, i) => new RankingRow(i + 1, e.Id, e.Name, (byte)e.Hero, e.Time, e.Score, e.Level)).ToList();

        var mine = top.FirstOrDefault(e => e.Person == person);

        if (mine is null)
        {
            var own = await query.Where(e => e.entry.PersonId == person)
                .Select(e => new { e.person.Id, e.person.Name, e.person.Hero, e.person.Level, e.entry.Time, e.entry.Score })
                .FirstOrDefaultAsync();

            if (own is not null)
            {
                var better = IsTimed(kind)
                    ? await database.RankingEntries.CountAsync(e => e.Kind == kind && e.Scope == scope && (e.Time < own.Time || (e.Time == own.Time && e.Score > own.Score)))
                    : await database.RankingEntries.CountAsync(e => e.Kind == kind && e.Scope == scope && e.Score > own.Score);

                mine = new RankingRow(better + 1, own.Id, own.Name, (byte)own.Hero, own.Time, own.Score, own.Level);
            }
        }

        return (top, mine);
    }
}

/// <summary>
/// The tower clears of a person and of its friends, for the ranking window of the tower.
/// </summary>
public static class TowerRankingStore
{
    /// <summary>
    /// The fastest clear of every stage of the tower by the person and by each of its friends.
    /// </summary>
    public static async Task<List<TowerClear>> TowerClearsAsync(this IDbContextFactory<PersistenceContext> factory, int person)
    {
        await using var database = await factory.CreateDbContextAsync();

        var friends = database.Friends
            .AsNoTracking()
            .Where(e => e.OwnerId == person && e.Kind == FriendKind.Friend)
            .Select(e => e.TargetId);

        return await database.RankingEntries
            .AsNoTracking()
            .Where(e => e.Kind == RankingKind.InfiniteTower && (e.PersonId == person || friends.Contains(e.PersonId)))
            .Join(database.Persons.AsNoTracking(), e => e.PersonId, e => e.Id, (entry, owner) => new TowerClear(
                owner.Id, owner.Name, (byte)owner.Hero, owner.Level, entry.Scope, entry.Time, entry.Id))
            .ToListAsync();
    }
}

/// <summary>
/// The rankings as the game sees them (<see cref="IRankingStore"/>), over <see cref="RankingStore"/>.
/// </summary>
public sealed class RankingBook(IDbContextFactory<PersistenceContext> factory) : IRankingStore
{
    public Task<bool> SubmitAsync(RankingKind kind, int scope, int person, int time, int score, CancellationToken cancellationToken = default)
    {
        return factory.SubmitRankingAsync(kind, scope, person, time, score);
    }

    public async Task<(IReadOnlyList<RankingRow> Top, RankingRow? Mine)> RankingAsync(RankingKind kind, int scope, int person, CancellationToken cancellationToken = default)
    {
        var (top, mine) = await factory.RankingAsync(kind, scope, person);

        return (top, mine);
    }
}
