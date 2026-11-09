using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Persistent;

namespace OpenWorker.Persistence;

/// <param name="Level">The level of the person as its row keeps it.</param>
public sealed record RankingRow(int Order, int Person, string Name, byte Hero, int Time, int Score, byte Level = 1);

/// <summary>
/// The rankings: who cleared a map the fastest, and who has the most kills.
/// </summary>
public static class RankingStore
{
    public const int PageSize = 50;

    /// <summary>
    /// Records a result for the person, keeping the better of it and the old one. A faster clear is better in a time
    /// attack (and with the same time, a higher score); a higher score is better in the arena.
    /// </summary>
    public static async Task SubmitRankingAsync(this IDbContextFactory<PersistenceContext> factory, RankingKind kind, int scope, int person, int time, int score)
    {
        await using var database = await factory.CreateDbContextAsync();

        var entry = await database.RankingEntries.AsTracking().FirstOrDefaultAsync(e => e.Kind == kind && e.Scope == scope && e.PersonId == person);

        if (entry is null)
        {
            database.RankingEntries.Add(new RankingEntryPersistent { Kind = kind, Scope = scope, PersonId = person, Time = time, Score = score });
        }

        else if (kind == RankingKind.TimeAttack
                     ? time < entry.Time || (time == entry.Time && score > entry.Score)
                     : score > entry.Score)
        {
            entry.Time = time;
            entry.Score = score;
        }

        else
        {
            return;
        }

        try
        {
            await database.SaveChangesAsync();
        }

        catch (DbUpdateException)
        {
            // Two results of the same person at once, the other one stays.
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

        var ordered = kind == RankingKind.TimeAttack
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
                var better = kind == RankingKind.TimeAttack
                    ? await database.RankingEntries.CountAsync(e => e.Kind == kind && e.Scope == scope && (e.Time < own.Time || (e.Time == own.Time && e.Score > own.Score)))
                    : await database.RankingEntries.CountAsync(e => e.Kind == kind && e.Scope == scope && e.Score > own.Score);

                mine = new RankingRow(better + 1, own.Id, own.Name, (byte)own.Hero, own.Time, own.Score, own.Level);
            }
        }

        return (top, mine);
    }
}
