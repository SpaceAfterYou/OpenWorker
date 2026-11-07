using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Persistent;

namespace OpenWorker.Persistence;

/// <summary>
/// The numbers a league runs on.
/// </summary>
public static class LeagueRules
{
    public const byte BasePosition = 0;
    public const byte SubMaster = 7;
    public const byte Master = 100;

    public const int MinNameLength = 2;
    public const int MaxNameLength = 8;
    public const int MaxPositionNameLength = 4;
    public const int MaxNoticeLength = 800;
    public const int MaxRecruitNoticeLength = 50;
    public const int MaxBoardLength = 60;
    public const int MaxBoardEntries = 30;
    public const int MaxApplicants = 30;
    public const int MinSearchLength = 2;
    public const int PageSize = 10;

    public const int MinLevel = 20;
    public const long CreateCost = 100_000;

    public static TimeSpan InviteLifetime { get; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// A person who left or was thrown out cannot join another league for this long.
    /// </summary>
    public static TimeSpan WithdrawPenalty { get; } = TimeSpan.FromHours(1);

    /// <summary>
    /// A master who disbanded a league cannot found another for this long.
    /// </summary>
    public static TimeSpan DisbandPenalty { get; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Only the master and the sub master can appoint or be appointed to the position; this many people hold the ones in between.
    /// </summary>
    public static bool CanBeGiven(byte position) => position is BasePosition or 1 or 2 or 3 or SubMaster;

    public static int AuthSlot(byte position) => position == Master ? LeaguePersistent.AuthCount - 1 : Math.Min((int)position, LeaguePersistent.AuthCount - 2);
}

/// <summary>
/// What a position may do. A bit each, set by the master for every position but the master's own.
/// </summary>
[Flags]
public enum LeagueRight
{
    None = 0,
    Invite = 1,
    Applicants = 2,
    Kick = 4,
    Notice = 8,
    RecruitNotice = 16,
    Open = 32,
    Board = 64,
    All = Invite | Applicants | Kick | Notice | RecruitNotice | Open | Board
}

public sealed record LeagueRecord(
    int Id,
    string Name,
    int MasterId,
    string MasterName,
    string SubMasterName,
    byte Rating,
    long Exp,
    long Money,
    bool Open,
    short Emblem,
    short Border,
    string Notice,
    DateTime? NoticeAt,
    string RecruitNotice,
    DateTime? RecruitNoticeAt,
    string[] PositionNames,
    int[] Auth,
    int[] LimitGoldOut,
    DateTime CreatedAt,
    int MemberCount)
{
    public bool Allows(byte position, LeagueRight right)
    {
        return position == LeagueRules.Master || (Auth[LeagueRules.AuthSlot(position)] & (int)right) != 0;
    }
}

public sealed record LeagueMemberRecord(int Person, int League, string Name, byte Hero, byte Position, long Exp, DateTime JoinedAt, DateTime? LastLogout);

public sealed record LeagueApplicantRecord(int Person, int League, string Name, byte Hero, DateTime At);

public sealed record LeagueBoardRecord(int Serial, int League, int Person, string Name, string Message, DateTime At);

public enum LeagueCreateOutcome
{
    Created,
    NameTaken,
    InLeague,

    /// <summary>The master disbanded a league not long ago.</summary>
    Cooldown
}

public enum LeagueJoinOutcome
{
    Joined,
    NoLeague,
    Full,
    InLeague,

    /// <summary>The person left a league not long ago.</summary>
    Penalty
}

public enum LeagueApplyOutcome
{
    Applied,
    NoLeague,
    Closed,
    InLeague,
    AlreadyApplied,
    ListFull,
    Penalty
}

/// <summary>
/// Leagues, their members, applicants and boards. A league is in the database and changes there, whichever server
/// asks; the ones who need to know are told by the service.
/// </summary>
public static class LeagueStore
{
    public static async Task<LeagueRecord?> LeagueAsync(this IDbContextFactory<PersistenceContext> factory, int league)
    {
        await using var database = await factory.CreateDbContextAsync();

        var entity = await database.Leagues.AsNoTracking().FirstOrDefaultAsync(e => e.Id == league);

        return entity is null ? null : (await RecordsAsync(database, [entity]))[0];
    }

    public static async Task<LeagueRecord?> LeagueOfAsync(this IDbContextFactory<PersistenceContext> factory, int person)
    {
        await using var database = await factory.CreateDbContextAsync();

        var entity = await database.LeagueMembers
            .AsNoTracking()
            .Where(e => e.PersonId == person)
            .Join(database.Leagues.AsNoTracking(), e => e.LeagueId, e => e.Id, (member, league) => league)
            .FirstOrDefaultAsync();

        return entity is null ? null : (await RecordsAsync(database, [entity]))[0];
    }

    public static async Task<LeagueMemberRecord?> MemberAsync(this IDbContextFactory<PersistenceContext> factory, int person)
    {
        await using var database = await factory.CreateDbContextAsync();

        var members = await QueryMembersAsync(database, e => e.PersonId == person, false, 1);

        return members.Count == 0 ? null : members[0];
    }

    public static async Task<List<LeagueMemberRecord>> MembersAsync(this IDbContextFactory<PersistenceContext> factory, int league)
    {
        await using var database = await factory.CreateDbContextAsync();

        return await QueryMembersAsync(database, e => e.LeagueId == league, true, int.MaxValue);
    }

    public static async Task<List<int>> MemberIdsAsync(this IDbContextFactory<PersistenceContext> factory, int league)
    {
        await using var database = await factory.CreateDbContextAsync();

        return await database.LeagueMembers.AsNoTracking().Where(e => e.LeagueId == league).Select(e => e.PersonId).ToListAsync();
    }

    public static async Task<List<LeagueApplicantRecord>> ApplicantsAsync(this IDbContextFactory<PersistenceContext> factory, int league)
    {
        await using var database = await factory.CreateDbContextAsync();

        return await database.LeagueApplicants
            .AsNoTracking()
            .Where(e => e.LeagueId == league)
            .Join(database.Persons.AsNoTracking(), e => e.PersonId, e => e.Id, (applicant, person) => new { applicant, person })
            .OrderBy(e => e.applicant.CreatedAt)
            .Select(e => new LeagueApplicantRecord(e.person.Id, e.applicant.LeagueId, e.person.Name, (byte)e.person.Hero, e.applicant.CreatedAt))
            .ToListAsync();
    }

    public static async Task<LeagueApplicantRecord?> ApplicantAsync(this IDbContextFactory<PersistenceContext> factory, int league, int person)
    {
        var applicants = await factory.ApplicantsAsync(league);

        return applicants.FirstOrDefault(e => e.Person == person);
    }

    public static async Task<List<LeagueBoardRecord>> BoardAsync(this IDbContextFactory<PersistenceContext> factory, int league)
    {
        await using var database = await factory.CreateDbContextAsync();

        return await database.LeagueBoards
            .AsNoTracking()
            .Where(e => e.LeagueId == league)
            .Join(database.Persons.AsNoTracking(), e => e.PersonId, e => e.Id, (note, person) => new { note, person })
            .OrderBy(e => e.note.Id)
            .Select(e => new LeagueBoardRecord(e.note.Id, e.note.LeagueId, e.person.Id, e.person.Name, e.note.Message, e.note.CreatedAt))
            .ToListAsync();
    }

    public static async Task<bool> NameTakenAsync(this IDbContextFactory<PersistenceContext> factory, string name)
    {
        await using var database = await factory.CreateDbContextAsync();

        var key = name.Trim().ToLower();

        return await database.Leagues.AnyAsync(e => e.Name.ToLower() == key);
    }

    /// <summary>
    /// When the person can found a league again, or null if they can.
    /// </summary>
    public static async Task<TimeSpan?> CreateCooldownAsync(this IDbContextFactory<PersistenceContext> factory, int person)
    {
        await using var database = await factory.CreateDbContextAsync();

        var until = await database.Persons.AsNoTracking().Where(e => e.Id == person).Select(e => e.LeagueCreateBlockedUntil).FirstOrDefaultAsync();

        return Remaining(until);
    }

    public static async Task<(LeagueCreateOutcome Outcome, TimeSpan Remain, LeagueRecord? League)> CreateAsync(
        this IDbContextFactory<PersistenceContext> factory,
        string name,
        int master,
        int[] auth)
    {
        await using var database = await factory.CreateDbContextAsync();
        await using var transaction = await database.Database.BeginTransactionAsync();

        var blocked = await database.Persons.AsNoTracking().Where(e => e.Id == master).Select(e => e.LeagueCreateBlockedUntil).FirstOrDefaultAsync();

        if (Remaining(blocked) is { } remain)
        {
            return (LeagueCreateOutcome.Cooldown, remain, null);
        }

        if (await database.LeagueMembers.AnyAsync(e => e.PersonId == master))
        {
            return (LeagueCreateOutcome.InLeague, TimeSpan.Zero, null);
        }

        var key = name.Trim().ToLower();

        if (await database.Leagues.AnyAsync(e => e.Name.ToLower() == key))
        {
            return (LeagueCreateOutcome.NameTaken, TimeSpan.Zero, null);
        }

        var league = new LeaguePersistent
        {
            Name = name.Trim(),
            MasterId = master,
            Auth = auth,
            LimitGoldOut = new int[LeaguePersistent.AuthCount]
        };

        database.Leagues.Add(league);

        try
        {
            await database.SaveChangesAsync();

            database.LeagueMembers.Add(new LeagueMemberPersistent { PersonId = master, LeagueId = league.Id, Position = LeagueRules.Master });
            await database.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        catch (DbUpdateException)
        {
            // Another league took the name at the same moment.
            return (LeagueCreateOutcome.NameTaken, TimeSpan.Zero, null);
        }

        return (LeagueCreateOutcome.Created, TimeSpan.Zero, (await RecordsAsync(database, [league]))[0]);
    }

    /// <summary>
    /// Takes the league and everything in it out of the database, and keeps its master from founding another for a while.
    /// </summary>
    public static async Task DisbandAsync(this IDbContextFactory<PersistenceContext> factory, int league, int master)
    {
        await using var database = await factory.CreateDbContextAsync();
        await using var transaction = await database.Database.BeginTransactionAsync();

        await database.Leagues.Where(e => e.Id == league).ExecuteDeleteAsync();

        await database.Persons
            .Where(e => e.Id == master)
            .ExecuteUpdateAsync(e => e.SetProperty(x => x.LeagueCreateBlockedUntil, DateTime.UtcNow + LeagueRules.DisbandPenalty));

        await transaction.CommitAsync();
    }

    public static async Task<LeagueJoinOutcome> JoinAsync(this IDbContextFactory<PersistenceContext> factory, int league, int person, int maxMembers)
    {
        await using var database = await factory.CreateDbContextAsync();
        await using var transaction = await database.Database.BeginTransactionAsync();

        if (!await database.Leagues.AnyAsync(e => e.Id == league))
        {
            return LeagueJoinOutcome.NoLeague;
        }

        if (await database.LeagueMembers.AnyAsync(e => e.PersonId == person))
        {
            return LeagueJoinOutcome.InLeague;
        }

        var blocked = await database.Persons.AsNoTracking().Where(e => e.Id == person).Select(e => e.LeagueJoinBlockedUntil).FirstOrDefaultAsync();

        if (Remaining(blocked) is not null)
        {
            return LeagueJoinOutcome.Penalty;
        }

        if (await database.LeagueMembers.CountAsync(e => e.LeagueId == league) >= maxMembers)
        {
            return LeagueJoinOutcome.Full;
        }

        database.LeagueMembers.Add(new LeagueMemberPersistent { PersonId = person, LeagueId = league });

        // Whatever the person asked for before is answered now.
        await database.LeagueApplicants.Where(e => e.PersonId == person).ExecuteDeleteAsync();

        try
        {
            await database.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        catch (DbUpdateException)
        {
            return LeagueJoinOutcome.InLeague;
        }

        return LeagueJoinOutcome.Joined;
    }

    /// <summary>
    /// Takes a person out of their league. The penalty keeps them from joining another for a while.
    /// </summary>
    public static async Task<bool> LeaveAsync(this IDbContextFactory<PersistenceContext> factory, int person, bool penalty)
    {
        await using var database = await factory.CreateDbContextAsync();

        var removed = await database.LeagueMembers.Where(e => e.PersonId == person).ExecuteDeleteAsync() > 0;

        if (removed && penalty)
        {
            await database.Persons
                .Where(e => e.Id == person)
                .ExecuteUpdateAsync(e => e.SetProperty(x => x.LeagueJoinBlockedUntil, DateTime.UtcNow + LeagueRules.WithdrawPenalty));
        }

        return removed;
    }

    public static async Task<TimeSpan?> JoinCooldownAsync(this IDbContextFactory<PersistenceContext> factory, int person)
    {
        await using var database = await factory.CreateDbContextAsync();

        var until = await database.Persons.AsNoTracking().Where(e => e.Id == person).Select(e => e.LeagueJoinBlockedUntil).FirstOrDefaultAsync();

        return Remaining(until);
    }

    public static async Task<LeagueApplyOutcome> ApplyAsync(this IDbContextFactory<PersistenceContext> factory, int league, int person)
    {
        await using var database = await factory.CreateDbContextAsync();
        await using var transaction = await database.Database.BeginTransactionAsync();

        var entity = await database.Leagues.AsNoTracking().FirstOrDefaultAsync(e => e.Id == league);

        if (entity is null)
        {
            return LeagueApplyOutcome.NoLeague;
        }

        if (!entity.Open)
        {
            return LeagueApplyOutcome.Closed;
        }

        if (await database.LeagueMembers.AnyAsync(e => e.PersonId == person))
        {
            return LeagueApplyOutcome.InLeague;
        }

        var blocked = await database.Persons.AsNoTracking().Where(e => e.Id == person).Select(e => e.LeagueJoinBlockedUntil).FirstOrDefaultAsync();

        if (Remaining(blocked) is not null)
        {
            return LeagueApplyOutcome.Penalty;
        }

        if (await database.LeagueApplicants.AnyAsync(e => e.LeagueId == league && e.PersonId == person))
        {
            return LeagueApplyOutcome.AlreadyApplied;
        }

        if (await database.LeagueApplicants.CountAsync(e => e.LeagueId == league) >= LeagueRules.MaxApplicants)
        {
            return LeagueApplyOutcome.ListFull;
        }

        database.LeagueApplicants.Add(new LeagueApplicantPersistent { LeagueId = league, PersonId = person });

        try
        {
            await database.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        catch (DbUpdateException)
        {
            return LeagueApplyOutcome.AlreadyApplied;
        }

        return LeagueApplyOutcome.Applied;
    }

    public static async Task<bool> RemoveApplicantAsync(this IDbContextFactory<PersistenceContext> factory, int league, int person)
    {
        await using var database = await factory.CreateDbContextAsync();

        return await database.LeagueApplicants.Where(e => e.LeagueId == league && e.PersonId == person).ExecuteDeleteAsync() > 0;
    }

    public static async Task<List<int>> ApplicationsOfAsync(this IDbContextFactory<PersistenceContext> factory, int person)
    {
        await using var database = await factory.CreateDbContextAsync();

        return await database.LeagueApplicants.AsNoTracking().Where(e => e.PersonId == person).Select(e => e.LeagueId).ToListAsync();
    }

    public static async Task SetNoticeAsync(this IDbContextFactory<PersistenceContext> factory, int league, string notice)
    {
        await using var database = await factory.CreateDbContextAsync();

        await database.Leagues
            .Where(e => e.Id == league)
            .ExecuteUpdateAsync(e => e.SetProperty(x => x.Notice, notice).SetProperty(x => x.NoticeAt, DateTime.UtcNow));
    }

    public static async Task SetRecruitNoticeAsync(this IDbContextFactory<PersistenceContext> factory, int league, string notice)
    {
        await using var database = await factory.CreateDbContextAsync();

        await database.Leagues
            .Where(e => e.Id == league)
            .ExecuteUpdateAsync(e => e.SetProperty(x => x.RecruitNotice, notice).SetProperty(x => x.RecruitNoticeAt, DateTime.UtcNow));
    }

    public static async Task SetOpenAsync(this IDbContextFactory<PersistenceContext> factory, int league, bool open)
    {
        await using var database = await factory.CreateDbContextAsync();

        await database.Leagues.Where(e => e.Id == league).ExecuteUpdateAsync(e => e.SetProperty(x => x.Open, open));
    }

    public static async Task SetAuthAsync(this IDbContextFactory<PersistenceContext> factory, int league, int[] auth, int[] limit)
    {
        await using var database = await factory.CreateDbContextAsync();

        await database.Leagues
            .Where(e => e.Id == league)
            .ExecuteUpdateAsync(e => e.SetProperty(x => x.Auth, auth).SetProperty(x => x.LimitGoldOut, limit));
    }

    public static async Task SetPositionNameAsync(this IDbContextFactory<PersistenceContext> factory, int league, int index, string name)
    {
        await using var database = await factory.CreateDbContextAsync();

        var entity = await database.Leagues.FirstOrDefaultAsync(e => e.Id == league);

        if (entity is null)
        {
            return;
        }

        var names = (string[])entity.PositionNames.Clone();
        names[index] = name;
        entity.PositionNames = names;

        await database.SaveChangesAsync();
    }

    /// <summary>
    /// Gives the member a position. At most one member is sub master.
    /// </summary>
    public static async Task<bool> SetPositionAsync(this IDbContextFactory<PersistenceContext> factory, int league, int person, byte position)
    {
        await using var database = await factory.CreateDbContextAsync();
        await using var transaction = await database.Database.BeginTransactionAsync();

        if (position == LeagueRules.SubMaster &&
            await database.LeagueMembers.AnyAsync(e => e.LeagueId == league && e.Position == LeagueRules.SubMaster && e.PersonId != person))
        {
            return false;
        }

        var changed = await database.LeagueMembers
            .Where(e => e.LeagueId == league && e.PersonId == person && e.Position != LeagueRules.Master)
            .ExecuteUpdateAsync(e => e.SetProperty(x => x.Position, position));

        await transaction.CommitAsync();

        return changed > 0;
    }

    /// <summary>
    /// Adds a note to the board, and drops the oldest when there are too many.
    /// </summary>
    public static async Task<LeagueBoardRecord> AddBoardAsync(this IDbContextFactory<PersistenceContext> factory, int league, int person, string name, string message)
    {
        await using var database = await factory.CreateDbContextAsync();
        await using var transaction = await database.Database.BeginTransactionAsync();

        var note = new LeagueBoardPersistent { LeagueId = league, PersonId = person, Message = message };

        database.LeagueBoards.Add(note);
        await database.SaveChangesAsync();

        var oldest = await database.LeagueBoards
            .Where(e => e.LeagueId == league)
            .OrderByDescending(e => e.Id)
            .Skip(LeagueRules.MaxBoardEntries)
            .Select(e => e.Id)
            .ToListAsync();

        if (oldest.Count > 0)
        {
            await database.LeagueBoards.Where(e => oldest.Contains(e.Id)).ExecuteDeleteAsync();
        }

        await transaction.CommitAsync();

        return new LeagueBoardRecord(note.Id, league, person, name, message, note.CreatedAt);
    }

    /// <summary>
    /// Leagues whose name or master's name has the text in it, the biggest first.
    /// </summary>
    public static async Task<List<LeagueRecord>> SearchAsync(this IDbContextFactory<PersistenceContext> factory, string name, string master, int limit)
    {
        await using var database = await factory.CreateDbContextAsync();

        var query = database.Leagues.AsNoTracking().AsQueryable();

        if (name.Trim().Length > 0)
        {
            var pattern = Pattern(name);

            query = query.Where(e => EF.Functions.ILike(e.Name, pattern));
        }

        if (master.Trim().Length > 0)
        {
            var pattern = Pattern(master);

            query = query.Where(e => database.Persons.Any(p => p.Id == e.MasterId && EF.Functions.ILike(p.Name, pattern)));
        }

        var found = await query.OrderByDescending(e => e.Exp).ThenBy(e => e.Name).Take(limit).ToListAsync();

        return await RecordsAsync(database, found);
    }

    /// <summary>
    /// A page of the list: by size, or by experience for the ranking.
    /// </summary>
    public static async Task<List<LeagueRecord>> ListAsync(this IDbContextFactory<PersistenceContext> factory, bool ranking, int limit)
    {
        await using var database = await factory.CreateDbContextAsync();

        var found = ranking
            ? await database.Leagues.AsNoTracking().OrderByDescending(e => e.Exp).ThenBy(e => e.Name).Take(limit).ToListAsync()
            : await database.Leagues.AsNoTracking().OrderByDescending(e => database.LeagueMembers.Count(m => m.LeagueId == e.Id)).ThenBy(e => e.Name).Take(limit).ToListAsync();

        return await RecordsAsync(database, found);
    }

    private static async Task<List<LeagueMemberRecord>> QueryMembersAsync(
        PersistenceContext database,
        System.Linq.Expressions.Expression<Func<LeagueMemberPersistent, bool>> filter,
        bool ordered,
        int limit)
    {
        var query = database.LeagueMembers
            .AsNoTracking()
            .Where(filter)
            .Join(database.Persons.AsNoTracking(), e => e.PersonId, e => e.Id, (member, person) => new { member, person });

        if (ordered)
        {
            query = query.OrderByDescending(e => e.member.Position).ThenBy(e => e.member.JoinedAt);
        }

        return await query
            .Take(limit)
            .Select(e => new LeagueMemberRecord(
                e.person.Id,
                e.member.LeagueId,
                e.person.Name,
                (byte)e.person.Hero,
                e.member.Position,
                e.member.Exp,
                e.member.JoinedAt,
                e.person.LastLogoutAt))
            .ToListAsync();
    }

    private static async Task<List<LeagueRecord>> RecordsAsync(PersistenceContext database, List<LeaguePersistent> leagues)
    {
        var ids = leagues.Select(e => e.Id).ToList();
        var masters = leagues.Select(e => e.MasterId).ToList();

        var names = await database.Persons.AsNoTracking().Where(e => masters.Contains(e.Id)).ToDictionaryAsync(e => e.Id, e => e.Name);

        var counts = await database.LeagueMembers
            .AsNoTracking()
            .Where(e => ids.Contains(e.LeagueId))
            .GroupBy(e => e.LeagueId)
            .Select(e => new { League = e.Key, Count = e.Count() })
            .ToDictionaryAsync(e => e.League, e => e.Count);

        var subs = await database.LeagueMembers
            .AsNoTracking()
            .Where(e => ids.Contains(e.LeagueId) && e.Position == LeagueRules.SubMaster)
            .Join(database.Persons.AsNoTracking(), e => e.PersonId, e => e.Id, (member, person) => new { member.LeagueId, person.Name })
            .ToListAsync();

        return leagues
            .Select(e => new LeagueRecord(
                e.Id,
                e.Name,
                e.MasterId,
                names.GetValueOrDefault(e.MasterId, string.Empty),
                subs.FirstOrDefault(x => x.LeagueId == e.Id)?.Name ?? string.Empty,
                e.Rating,
                e.Exp,
                e.Money,
                e.Open,
                e.CardEmblem,
                e.CardBorder,
                e.Notice,
                e.NoticeAt,
                e.RecruitNotice,
                e.RecruitNoticeAt,
                e.PositionNames,
                e.Auth,
                e.LimitGoldOut,
                e.CreatedAt,
                counts.GetValueOrDefault(e.Id)))
            .ToList();
    }

    private static string Pattern(string text)
    {
        return "%" + text.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
    }

    private static TimeSpan? Remaining(DateTime? until)
    {
        return until is { } time && time.ToUniversalTime() > DateTime.UtcNow ? time.ToUniversalTime() - DateTime.UtcNow : null;
    }
}
