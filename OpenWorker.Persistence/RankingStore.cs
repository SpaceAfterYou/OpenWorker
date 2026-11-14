using Microsoft.EntityFrameworkCore;
using Npgsql;
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
    public static async Task<bool> SubmitRankingAsync(this IDbContextFactory<PersistenceContext> factory, RankingKind kind, int scope, int person, int time, int score, DateTime now)
    {
        await using var database = await factory.CreateDbContextAsync();

        var entry = await database.RankingEntries.AsTracking().FirstOrDefaultAsync(e => e.Kind == kind && e.Scope == scope && e.PersonId == person);

        if (entry is null)
        {
            database.RankingEntries.Add(new RankingEntryPersistent { Kind = kind, Scope = scope, PersonId = person, Time = time, Score = score, RecordedAt = now });
        }

        else if (IsTimed(kind)
                     ? time < entry.Time || (time == entry.Time && score > entry.Score)
                     : score > entry.Score)
        {
            entry.Time = time;
            entry.Score = score;
            entry.RecordedAt = now;
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

        var query = Entries(database, kind, scope);

        var rows = await Ordered(query, kind)
            .Select(e => new { e.Person.Id, e.Person.Name, e.Person.Hero, e.Person.Level, e.Entry.Time, e.Entry.Score })
            .Take(PageSize)
            .ToListAsync();

        var top = rows.Select((e, i) => new RankingRow(i + 1, e.Id, e.Name, (byte)e.Hero, e.Time, e.Score, e.Level)).ToList();

        var mine = top.FirstOrDefault(e => e.Person == person);

        if (mine is null)
        {
            var own = await query.Where(e => e.Entry.PersonId == person)
                .Select(e => new { e.Person.Id, e.Person.Name, e.Person.Hero, e.Person.Level, e.Entry.Time, e.Entry.Score })
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

    /// <summary>
    /// The first places of the period that ends at <paramref name="end"/>, with the accounts of their persons; null when
    /// the period has ended already.
    /// </summary>
    public static async Task<List<RankingPlace>?> PeriodPlacesAsync(
        this IDbContextFactory<PersistenceContext> factory,
        RankingKind kind,
        int scope,
        DateTime end,
        int places)
    {
        await using var database = await factory.CreateDbContextAsync();

        if (await database.RankingPeriods.AnyAsync(e => e.Kind == kind && e.Scope == scope && e.EndedAt == end))
        {
            return null;
        }

        var rows = await Ordered(Entries(database, kind, scope).Where(e => e.Entry.RecordedAt < end), kind)
            .Select(e => new { e.Person.Id, Account = e.Person.Account.Id })
            .Take(places)
            .ToListAsync();

        return rows.Select((e, i) => new RankingPlace(i + 1, e.Id, e.Account)).ToList();
    }

    /// <summary>
    /// Ends the period at <paramref name="end"/> in one transaction: its row, the letters with its rewards, and the results
    /// recorded before it taken off the list. The letters as stored; null, with nothing written, when another server ended
    /// it first.
    /// </summary>
    public static async Task<List<PostPersistent>?> EndPeriodAsync(
        this IDbContextFactory<PersistenceContext> factory,
        RankingKind kind,
        int scope,
        DateTime end,
        IReadOnlyList<PostAccountDraft> letters,
        DateTime now)
    {
        await using var database = await factory.CreateDbContextAsync();
        await using var transaction = await database.Database.BeginTransactionAsync();

        database.RankingPeriods.Add(new RankingPeriodPersistent { Kind = kind, Scope = scope, EndedAt = end, Letters = letters.Count, CreatedAt = now, UpdatedAt = now });

        var written = letters.Select(draft => new PostPersistent
        {
            Kind = PostKind.Account,
            SenderName = draft.SenderName,
            ReceiverAccountId = draft.Account,
            Title = draft.Title,
            Body = draft.Body,
            Money = draft.Money,
            Items = draft.Items.ToList(),
            MainType = draft.MainType,
            SubType = draft.SubType,
            State = draft.Money == 0 && draft.Items.Count == 0 ? PostState.Received : PostState.None,
            CreatedAt = now,
            UpdatedAt = now,
            ExpiresAt = now + PostLimits.Lifetime
        }).ToList();

        database.Posts.AddRange(written);

        try
        {
            await database.SaveChangesAsync();
        }

        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return null;
        }

        await database.RankingEntries
            .Where(e => e.Kind == kind && e.Scope == scope && e.RecordedAt < end)
            .ExecuteDeleteAsync();

        await transaction.CommitAsync();

        return written;
    }

    /// <summary>
    /// The order of a ranking: the fastest clear first (with the same time, the higher score), or the highest score; then
    /// whoever was in it first.
    /// </summary>
    private static IOrderedQueryable<Joined> Ordered(IQueryable<Joined> query, RankingKind kind)
    {
        return IsTimed(kind)
            ? query.OrderBy(e => e.Entry.Time).ThenByDescending(e => e.Entry.Score).ThenBy(e => e.Entry.Id)
            : query.OrderByDescending(e => e.Entry.Score).ThenBy(e => e.Entry.Id);
    }

    private static IQueryable<Joined> Entries(PersistenceContext database, RankingKind kind, int scope)
    {
        return database.RankingEntries
            .AsNoTracking()
            .Where(e => e.Kind == kind && e.Scope == scope)
            .Join(database.Persons.AsNoTracking(), e => e.PersonId, e => e.Id, (entry, person) => new Joined { Entry = entry, Person = person });
    }

    /// <summary>
    /// A result with its person.
    /// </summary>
    private sealed class Joined
    {
        public required RankingEntryPersistent Entry { get; init; }

        public required PersonPersistent Person { get; init; }
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
public sealed class RankingBook(IDbContextFactory<PersistenceContext> factory, TimeProvider? clock = null) : IRankingStore
{
    private DateTime Now => (clock ?? TimeProvider.System).GetUtcNow().UtcDateTime;

    public Task<bool> SubmitAsync(RankingKind kind, int scope, int person, int time, int score, CancellationToken cancellationToken = default)
    {
        return factory.SubmitRankingAsync(kind, scope, person, time, score, Now);
    }

    public async Task<(IReadOnlyList<RankingRow> Top, RankingRow? Mine)> RankingAsync(RankingKind kind, int scope, int person, CancellationToken cancellationToken = default)
    {
        var (top, mine) = await factory.RankingAsync(kind, scope, person);

        return (top, mine);
    }

    public async Task<IReadOnlyList<RankingPlace>?> PeriodPlacesAsync(RankingKind kind, int scope, DateTime endedAt, int places, CancellationToken cancellationToken = default)
    {
        return await factory.PeriodPlacesAsync(kind, scope, endedAt, places);
    }

    public async Task<IReadOnlyList<PostPersistent>?> EndPeriodAsync(RankingKind kind, int scope, DateTime endedAt, IReadOnlyList<PostAccountDraft> letters, CancellationToken cancellationToken = default)
    {
        return await factory.EndPeriodAsync(kind, scope, endedAt, letters, Now);
    }
}
