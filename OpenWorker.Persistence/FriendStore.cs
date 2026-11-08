using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Persistent;

namespace OpenWorker.Persistence;

/// <param name="Memo">What the person wrote about themselves.</param>
/// <param name="Since">When the entry was made.</param>
public readonly record struct FriendEntry(
    int Person,
    string Name,
    byte Hero,
    FriendKind Kind,
    long Point,
    byte CommunityState,
    string Memo,
    DateTime? LastLogout,
    DateTime Since);

public readonly record struct PersonCard(int Person, string Name, byte Hero);

public enum InviteOutcome
{
    Sent,

    /// <summary>The invited person does not exist, or is the one who asks.</summary>
    Nobody,
    AlreadyFriend,
    AlreadyInvited,

    /// <summary>The invited person has the one who asks in their block list.</summary>
    Blocked,

    /// <summary>The one who asks has the invited person in their block list.</summary>
    YouBlocked,
    YouFull,
    TargetFull
}

public enum AnswerOutcome
{
    Befriended,
    Declined,

    /// <summary>Nobody asked, or the request is gone.</summary>
    NoRequest,
    YouFull,
    TargetFull
}

public enum BlockOutcome
{
    Done,
    Nobody,
    AlreadyBlocked,
    IsFriend,
    Full
}

/// <summary>
/// Friend lists, requests and block lists. Each person has a row for their own side of every relation, so a pair of
/// friends is two rows.
/// </summary>
public static class FriendStore
{
    public const int MaxFriends = 100;
    public const int MaxRequests = 20;
    public const int MaxInvites = 50;
    public const int MaxBlocks = 100;

    public static async Task<List<FriendEntry>> ListAsync(this IDbContextFactory<PersistenceContext> factory, int owner)
    {
        await using var database = await factory.CreateDbContextAsync();

        return await database.Friends
            .AsNoTracking()
            .Where(e => e.OwnerId == owner && e.Kind != FriendKind.Block)
            .Join(database.Persons, e => e.TargetId, e => e.Id, (friend, person) => new { friend, person })
            .OrderBy(e => e.friend.Kind)
            .ThenBy(e => e.person.Name)
            .Select(e => new FriendEntry(
                e.person.Id,
                e.person.Name,
                (byte)e.person.Hero,
                e.friend.Kind,
                e.friend.Point,
                e.person.CommunityState,
                e.person.CommunityMemo,
                e.person.LastLogoutAt,
                e.friend.CreatedAt))
            .ToListAsync();
    }

    public static async Task<List<PersonCard>> BlockedAsync(this IDbContextFactory<PersistenceContext> factory, int owner)
    {
        await using var database = await factory.CreateDbContextAsync();

        return await database.Friends
            .AsNoTracking()
            .Where(e => e.OwnerId == owner && e.Kind == FriendKind.Block)
            .Join(database.Persons, e => e.TargetId, e => e.Id, (friend, person) => new { friend, person })
            .OrderBy(e => e.person.Name)
            .Select(e => new PersonCard(e.person.Id, e.person.Name, (byte)e.person.Hero))
            .ToListAsync();
    }

    public static async Task<FriendEntry?> EntryAsync(this IDbContextFactory<PersistenceContext> factory, int owner, int target)
    {
        await using var database = await factory.CreateDbContextAsync();

        var entries = await database.Friends
            .AsNoTracking()
            .Where(e => e.OwnerId == owner && e.TargetId == target)
            .Join(database.Persons, e => e.TargetId, e => e.Id, (friend, person) => new FriendEntry(
                person.Id,
                person.Name,
                (byte)person.Hero,
                friend.Kind,
                friend.Point,
                person.CommunityState,
                person.CommunityMemo,
                person.LastLogoutAt,
                friend.CreatedAt))
            .Take(1)
            .ToListAsync();

        return entries.Count == 0 ? null : entries[0];
    }

    public static async Task<PersonCard?> FindPersonAsync(this IDbContextFactory<PersistenceContext> factory, string name)
    {
        await using var database = await factory.CreateDbContextAsync();

        var key = name.Trim().ToLowerInvariant();

        // The comparison runs in the database, as lower("Name") = @key. Npgsql translates only the ToLower() without arguments and
        // not string.Equals with a StringComparison, so the advice of the analyzers (culture, StringComparison) would not translate.
#pragma warning disable CA1304, CA1311, CA1862
        var cards = await database.Persons
            .AsNoTracking()
            .Where(e => e.Name.ToLower() == key)
            .Select(e => new PersonCard(e.Id, e.Name, (byte)e.Hero))
            .Take(1)
            .ToListAsync();
#pragma warning restore CA1304, CA1311, CA1862

        return cards.Count == 0 ? null : cards[0];
    }

    public static async Task<List<PersonCard>> SearchAsync(this IDbContextFactory<PersistenceContext> factory, string text, int skip, int limit)
    {
        await using var database = await factory.CreateDbContextAsync();

        var pattern = "%" + text.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";

        return await database.Persons
            .AsNoTracking()
            .Where(e => EF.Functions.ILike(e.Name, pattern))
            .OrderBy(e => e.Name)
            .Skip(skip)
            .Take(limit)
            .Select(e => new PersonCard(e.Id, e.Name, (byte)e.Hero))
            .ToListAsync();
    }

    /// <summary>
    /// Persons the owner has no entry for, to suggest as friends. Not the owner.
    /// </summary>
    public static async Task<List<PersonCard>> StrangersAsync(this IDbContextFactory<PersistenceContext> factory, int owner, int limit)
    {
        await using var database = await factory.CreateDbContextAsync();

        return await database.Persons
            .AsNoTracking()
            .Where(e => e.Id != owner
                        && !database.Friends.Any(f => f.OwnerId == owner && f.TargetId == e.Id)
                        && !database.Friends.Any(f => f.OwnerId == e.Id && f.TargetId == owner && f.Kind == FriendKind.Block))
            .OrderByDescending(e => e.LastLogoutAt ?? DateTime.MaxValue)
            .Take(limit)
            .Select(e => new PersonCard(e.Id, e.Name, (byte)e.Hero))
            .ToListAsync();
    }

    public static async Task<bool> IsFriendAsync(this IDbContextFactory<PersistenceContext> factory, int owner, int other)
    {
        await using var database = await factory.CreateDbContextAsync();

        return await database.Friends
            .AsNoTracking()
            .AnyAsync(e => e.OwnerId == owner && e.TargetId == other && e.Kind == FriendKind.Friend);
    }

    public static async Task<bool> IsBlockedAsync(this IDbContextFactory<PersistenceContext> factory, int owner, int other)
    {
        await using var database = await factory.CreateDbContextAsync();

        return await database.Friends
            .AsNoTracking()
            .AnyAsync(e => e.OwnerId == owner && e.TargetId == other && e.Kind == FriendKind.Block);
    }

    /// <summary>
    /// The friends of a person, each with what the friend has in their own list as points with the person. For telling them
    /// about a change. Pending requests do not count.
    /// </summary>
    public static async Task<Dictionary<int, long>> FriendPointsAsync(this IDbContextFactory<PersistenceContext> factory, int person)
    {
        await using var database = await factory.CreateDbContextAsync();

        return await database.Friends
            .AsNoTracking()
            .Where(e => e.TargetId == person && e.Kind == FriendKind.Friend)
            .ToDictionaryAsync(e => e.OwnerId, e => e.Point);
    }

    public static async Task<InviteOutcome> InviteAsync(this IDbContextFactory<PersistenceContext> factory, int inviter, int target)
    {
        if (inviter == target)
        {
            return InviteOutcome.Nobody;
        }

        await using var database = await factory.CreateDbContextAsync();
        await using var transaction = await database.Database.BeginTransactionAsync();

        if (!await database.Persons.AnyAsync(e => e.Id == target))
        {
            return InviteOutcome.Nobody;
        }

        var rows = await database.Friends
            .Where(e => (e.OwnerId == inviter && e.TargetId == target) || (e.OwnerId == target && e.TargetId == inviter))
            .ToListAsync();

        var mine = rows.FirstOrDefault(e => e.OwnerId == inviter);
        var theirs = rows.FirstOrDefault(e => e.OwnerId == target);

        if (theirs?.Kind == FriendKind.Block)
        {
            return InviteOutcome.Blocked;
        }

        switch (mine?.Kind)
        {
            case FriendKind.Block:
                return InviteOutcome.YouBlocked;

            case FriendKind.Friend:
                return InviteOutcome.AlreadyFriend;

            case FriendKind.Wait or FriendKind.Invite:
                return InviteOutcome.AlreadyInvited;
        }

        if (await CountAsync(database, inviter, FriendKind.Friend) >= MaxFriends ||
            await CountAsync(database, inviter, FriendKind.Invite) >= MaxInvites)
        {
            return InviteOutcome.YouFull;
        }

        if (await CountAsync(database, target, FriendKind.Wait) >= MaxRequests)
        {
            return InviteOutcome.TargetFull;
        }

        // Whatever is left of an earlier relation makes way for the request.
        database.Friends.RemoveRange(rows);
        await database.SaveChangesAsync();

        database.Friends.Add(new FriendPersistent { OwnerId = inviter, TargetId = target, Kind = FriendKind.Invite });
        database.Friends.Add(new FriendPersistent { OwnerId = target, TargetId = inviter, Kind = FriendKind.Wait });

        try
        {
            await database.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        catch (DbUpdateException)
        {
            // Both asked at the same moment.
            return InviteOutcome.AlreadyInvited;
        }

        return InviteOutcome.Sent;
    }

    /// <summary>
    /// The one who was asked answers.
    /// </summary>
    public static async Task<AnswerOutcome> AnswerAsync(this IDbContextFactory<PersistenceContext> factory, int answering, int inviter, bool accept)
    {
        await using var database = await factory.CreateDbContextAsync();
        await using var transaction = await database.Database.BeginTransactionAsync();

        var rows = await database.Friends
            .Where(e => (e.OwnerId == answering && e.TargetId == inviter) || (e.OwnerId == inviter && e.TargetId == answering))
            .ToListAsync();

        var mine = rows.FirstOrDefault(e => e.OwnerId == answering);
        var theirs = rows.FirstOrDefault(e => e.OwnerId == inviter);

        if (mine is not { Kind: FriendKind.Wait })
        {
            return AnswerOutcome.NoRequest;
        }

        if (!accept)
        {
            database.Friends.RemoveRange(rows);
            await database.SaveChangesAsync();
            await transaction.CommitAsync();

            return AnswerOutcome.Declined;
        }

        if (await CountAsync(database, answering, FriendKind.Friend) >= MaxFriends)
        {
            return AnswerOutcome.YouFull;
        }

        if (await CountAsync(database, inviter, FriendKind.Friend) >= MaxFriends)
        {
            return AnswerOutcome.TargetFull;
        }

        mine.Kind = FriendKind.Friend;

        if (theirs is null)
        {
            database.Friends.Add(new FriendPersistent { OwnerId = inviter, TargetId = answering, Kind = FriendKind.Friend });
        }

        else
        {
            theirs.Kind = FriendKind.Friend;
        }

        await database.SaveChangesAsync();
        await transaction.CommitAsync();

        return AnswerOutcome.Befriended;
    }

    /// <summary>
    /// Takes the other out of the list of the owner, and the owner out of theirs. A friend, or a request on either side.
    /// </summary>
    public static async Task<bool> RemoveAsync(this IDbContextFactory<PersistenceContext> factory, int owner, int other)
    {
        await using var database = await factory.CreateDbContextAsync();

        var removed = await database.Friends
            .Where(e => ((e.OwnerId == owner && e.TargetId == other) || (e.OwnerId == other && e.TargetId == owner))
                        && e.Kind != FriendKind.Block)
            .ExecuteDeleteAsync();

        return removed > 0;
    }

    public static async Task<BlockOutcome> BlockAsync(this IDbContextFactory<PersistenceContext> factory, int owner, int target)
    {
        if (owner == target)
        {
            return BlockOutcome.Nobody;
        }

        await using var database = await factory.CreateDbContextAsync();
        await using var transaction = await database.Database.BeginTransactionAsync();

        if (!await database.Persons.AnyAsync(e => e.Id == target))
        {
            return BlockOutcome.Nobody;
        }

        var mine = await database.Friends.FirstOrDefaultAsync(e => e.OwnerId == owner && e.TargetId == target);

        switch (mine?.Kind)
        {
            case FriendKind.Block:
                return BlockOutcome.AlreadyBlocked;

            case FriendKind.Friend:
                return BlockOutcome.IsFriend;
        }

        if (await CountAsync(database, owner, FriendKind.Block) >= MaxBlocks)
        {
            return BlockOutcome.Full;
        }

        // A request that is still open between them goes away with the block.
        await database.Friends
            .Where(e => ((e.OwnerId == owner && e.TargetId == target) || (e.OwnerId == target && e.TargetId == owner))
                        && e.Kind != FriendKind.Block)
            .ExecuteDeleteAsync();

        database.Friends.Add(new FriendPersistent { OwnerId = owner, TargetId = target, Kind = FriendKind.Block });

        await database.SaveChangesAsync();
        await transaction.CommitAsync();

        return BlockOutcome.Done;
    }

    public static async Task<bool> UnblockAsync(this IDbContextFactory<PersistenceContext> factory, int owner, int target)
    {
        await using var database = await factory.CreateDbContextAsync();

        return await database.Friends
            .Where(e => e.OwnerId == owner && e.TargetId == target && e.Kind == FriendKind.Block)
            .ExecuteDeleteAsync() > 0;
    }

    public static async Task SetLastLogoutAsync(this IDbContextFactory<PersistenceContext> factory, int person, DateTime when)
    {
        await using var database = await factory.CreateDbContextAsync();

        await database.Persons
            .Where(e => e.Id == person)
            .ExecuteUpdateAsync(e => e.SetProperty(x => x.LastLogoutAt, when));
    }

    private static Task<int> CountAsync(PersistenceContext database, int owner, FriendKind kind)
    {
        return database.Friends.CountAsync(e => e.OwnerId == owner && e.Kind == kind);
    }
}
