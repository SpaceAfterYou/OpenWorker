using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;

namespace OpenWorker.Persistence;

/// <summary>
/// The letters over the database. Every change is one statement that says in its condition what the letter must still be
/// (in the box of the one who asks, its attachments not taken), so two servers that change one letter at the same moment
/// cannot both succeed; what spans rows (a box that must not overflow) runs in a transaction that holds the row of the
/// person whose box it is.
/// </summary>
public sealed class PostStore(IDbContextFactory<PersistenceContext> factory, TimeProvider? time = null) : IPostStore
{
    private DateTime Now => (time ?? TimeProvider.System).GetUtcNow().UtcDateTime;

    public async Task<PostSendResult> SendAsync(PostDraft draft, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);

        var key = draft.ReceiverName.Trim().ToLowerInvariant();

        // The comparison runs in the database, as lower("Name") = @key (see FriendStore.FindPersonAsync).
#pragma warning disable CA1304, CA1311, CA1862
        var found = await database.Persons
            .Where(e => e.Name.ToLower() == key)
            .Select(e => new { e.Id, e.Name, e.Hero })
            .Take(1)
            .ToListAsync(cancellationToken);
#pragma warning restore CA1304, CA1311, CA1862

        if (found.Count == 0)
        {
            return new PostSendResult(PostSendOutcome.NoReceiver, null, 0);
        }

        var receiver = found[0];

        if (receiver.Id == draft.Sender)
        {
            return new PostSendResult(PostSendOutcome.Self, null, 0);
        }

        // Whom the receiver blocked does not reach them.
        if (await database.Friends.AnyAsync(e => e.OwnerId == receiver.Id && e.TargetId == draft.Sender && e.Kind == FriendKind.Block,
                cancellationToken))
        {
            return new PostSendResult(PostSendOutcome.NoReceiver, null, 0);
        }

        var now = Now;

        if (!await HoldBoxAsync(database, receiver.Id, now, cancellationToken))
        {
            return new PostSendResult(PostSendOutcome.Full, null, 0);
        }

        var letter = new PostPersistent
        {
            Kind = PostKind.Person,
            SenderId = draft.Sender,
            SenderName = draft.SenderName,
            SenderHero = draft.SenderHero,
            ReceiverId = receiver.Id,
            ReceiverName = receiver.Name,
            ReceiverHero = (byte)receiver.Hero,
            Title = draft.Title,
            Body = draft.Body,
            Money = draft.Money,
            Postage = draft.Postage,
            Items = draft.Items.ToList(),
            State = Nothing(draft.Money, draft.Items),
            CreatedAt = now,
            UpdatedAt = now,
            ExpiresAt = now + PostLimits.Lifetime
        };

        database.Posts.Add(letter);

        await database.SaveChangesAsync(cancellationToken);

        var unread = await Unread(database.Posts, receiver.Id, now).CountAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new PostSendResult(PostSendOutcome.Sent, letter, unread);
    }

    public async Task<PostPersistent> SendToAccountAsync(PostAccountDraft draft, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        var now = Now;

        var letter = new PostPersistent
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
            State = Nothing(draft.Money, draft.Items),
            CreatedAt = now,
            UpdatedAt = now,
            ExpiresAt = now + PostLimits.Lifetime
        };

        database.Posts.Add(letter);

        await database.SaveChangesAsync(cancellationToken);

        return letter;
    }

    public async Task<List<PostPersistent>> ListReceivedAsync(int person, bool saved, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        var box = Box(database.Posts, person, Now);

        box = saved
            ? box.Where(e => (e.State & PostState.Saved) != 0)
            : box.Where(e => (e.State & PostState.Saved) == 0);

        return await box
            .OrderByDescending(e => e.Id)
            .Take(PostLimits.MaxLetters)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PostPersistent>> ListSentAsync(int person, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        return await SentBox(database.Posts, person, Now)
            .OrderByDescending(e => e.Id)
            .Take(PostLimits.MaxLetters)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PostPersistent>> ListAccountAsync(int account, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        return await AccountBox(database.Posts, account, Now)
            .OrderByDescending(e => e.Id)
            .Take(PostLimits.MaxLetters)
            .ToListAsync(cancellationToken);
    }

    public async Task<(int Person, int Account)> CountUnreadAsync(int person, int account, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        var now = Now;
        var mine = await Unread(database.Posts, person, now).CountAsync(cancellationToken);
        var shared = await AccountBox(database.Posts, account, now).CountAsync(e => (e.State & PostState.Read) == 0, cancellationToken);

        return (mine, shared);
    }

    public async Task<PostPersistent?> ReadAsync(long serial, int person, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        var letters = Box(database.Posts, person, Now).Where(e => e.Id == serial);

        await MarkAsync(letters, PostState.Read, cancellationToken);

        return await letters.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PostPersistent?> ReadAccountAsync(long serial, int account, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        var letters = AccountBox(database.Posts, account, Now).Where(e => e.Id == serial);

        await MarkAsync(letters, PostState.Read, cancellationToken);

        return await letters.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PostPersistent?> ClaimAsync(long serial, int person, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        return await ClaimAsync(Box(database.Posts, person, Now).Where(e => e.Id == serial), cancellationToken);
    }

    public async Task<PostPersistent?> ClaimAccountAsync(long serial, int account, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        return await ClaimAsync(AccountBox(database.Posts, account, Now).Where(e => e.Id == serial), cancellationToken);
    }

    public async Task ReleaseAsync(long serial, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        var now = Now;

        await database.Posts
            .Where(e => e.Id == serial)
            .ExecuteUpdateAsync(e => e
                .SetProperty(x => x.State, x => x.State & ~PostState.Received)
                .SetProperty(x => x.UpdatedAt, now), cancellationToken);
    }

    public async Task<PostPersistent?> SaveAsync(long serial, int person, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        var letters = Box(database.Posts, person, Now).Where(e => e.Id == serial);

        if (await MarkAsync(letters, PostState.Saved | PostState.Read, cancellationToken) == 0)
        {
            return null;
        }

        return await letters.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PostDeleteOutcome> DeleteReceivedAsync(long serial, int person, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        var letters = Box(database.Posts, person, Now).Where(e => e.Id == serial);

        if (await MarkAsync(letters.Where(e => (e.State & PostState.Received) != 0), PostState.GoneForReceiver, cancellationToken) == 0)
        {
            return await letters.AnyAsync(cancellationToken) ? PostDeleteOutcome.HasAttachments : PostDeleteOutcome.Missing;
        }

        await DropForgottenAsync(database, serial, cancellationToken);

        return PostDeleteOutcome.Deleted;
    }

    public async Task<PostDeleteOutcome> DeleteSentAsync(long serial, int person, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        var letters = SentBox(database.Posts, person, Now).Where(e => e.Id == serial);

        // The receiver keeps the letter and what it carries.
        if (await MarkAsync(letters, PostState.GoneForSender, cancellationToken) == 0)
        {
            return PostDeleteOutcome.Missing;
        }

        await DropForgottenAsync(database, serial, cancellationToken);

        return PostDeleteOutcome.Deleted;
    }

    public async Task<PostDeleteOutcome> DeleteAccountAsync(long serial, int account, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        var letters = AccountBox(database.Posts, account, Now).Where(e => e.Id == serial);

        if (await letters.Where(e => (e.State & PostState.Received) != 0).ExecuteDeleteAsync(cancellationToken) == 0)
        {
            return await letters.AnyAsync(cancellationToken) ? PostDeleteOutcome.HasAttachments : PostDeleteOutcome.Missing;
        }

        return PostDeleteOutcome.Deleted;
    }

    public async Task<PostReturnResult> SendBackAsync(long serial, int person, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);

        var now = Now;
        var letters = Box(database.Posts, person, now).Where(e => e.Id == serial);
        var found = await letters.Select(e => new { e.SenderId, e.State }).Take(1).ToListAsync(cancellationToken);

        if (found.Count == 0)
        {
            return new PostReturnResult(PostReturnOutcome.Missing, null, 0);
        }

        if (found[0].SenderId is not { } sender)
        {
            return new PostReturnResult(PostReturnOutcome.FromTheGame, null, 0);
        }

        if ((found[0].State & PostState.Returned) != 0)
        {
            return new PostReturnResult(PostReturnOutcome.AlreadyReturned, null, 0);
        }

        if (!await HoldBoxAsync(database, sender, now, cancellationToken))
        {
            return new PostReturnResult(PostReturnOutcome.Full, null, 0);
        }

        // The two sides change places in one statement: the right side of every assignment is the row as it was. The one
        // who sends it back does not list it as sent; what was taken stays taken.
        var moved = await letters
            .Where(e => e.SenderId == sender && (e.State & PostState.Returned) == 0)
            .ExecuteUpdateAsync(e => e
                .SetProperty(x => x.ReceiverId, x => x.SenderId)
                .SetProperty(x => x.ReceiverName, x => x.SenderName)
                .SetProperty(x => x.ReceiverHero, x => x.SenderHero)
                .SetProperty(x => x.SenderId, x => x.ReceiverId)
                .SetProperty(x => x.SenderName, x => x.ReceiverName)
                .SetProperty(x => x.SenderHero, x => x.ReceiverHero)
                .SetProperty(x => x.State, x => (x.State & PostState.Received) | PostState.Returned | PostState.GoneForSender)
                .SetProperty(x => x.ExpiresAt, now + PostLimits.Lifetime)
                .SetProperty(x => x.UpdatedAt, now), cancellationToken);

        if (moved == 0)
        {
            return new PostReturnResult(PostReturnOutcome.Missing, null, 0);
        }

        var letter = await database.Posts.FirstAsync(e => e.Id == serial, cancellationToken);
        var unread = await Unread(database.Posts, sender, now).CountAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new PostReturnResult(PostReturnOutcome.Returned, letter, unread);
    }

    public async Task<int> SweepAsync(int limit, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        var now = Now;

        // One statement: a letter two servers sweep at the same moment is deleted by one of them and skipped by the other.
        return await database.Posts
            .Where(e => e.ExpiresAt <= now && ((e.State & PostState.Saved) == 0 || (e.State & PostState.GoneForReceiver) != 0))
            .OrderBy(e => e.ExpiresAt)
            .Take(limit)
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>
    /// The letters a person keeps, received and kept. A received letter whose time is up is gone for its receiver, whether
    /// the sweep has removed it yet or not; a kept one stays until the receiver deletes it.
    /// </summary>
    private static IQueryable<PostPersistent> Box(IQueryable<PostPersistent> posts, int person, DateTime now)
    {
        return posts.Where(e => e.Kind == PostKind.Person && e.ReceiverId == person && (e.State & PostState.GoneForReceiver) == 0 &&
                                ((e.State & PostState.Saved) != 0 || e.ExpiresAt > now));
    }

    private static IQueryable<PostPersistent> Unread(IQueryable<PostPersistent> posts, int person, DateTime now)
    {
        return Box(posts, person, now).Where(e => (e.State & PostState.Read) == 0);
    }

    /// <summary>
    /// The letters a person sent and still lists, until their time is up.
    /// </summary>
    private static IQueryable<PostPersistent> SentBox(IQueryable<PostPersistent> posts, int person, DateTime now)
    {
        return posts.Where(e => e.Kind == PostKind.Person && e.SenderId == person && (e.State & PostState.GoneForSender) == 0 && e.ExpiresAt > now);
    }

    private static IQueryable<PostPersistent> AccountBox(IQueryable<PostPersistent> posts, int account, DateTime now)
    {
        return posts.Where(e => e.Kind == PostKind.Account && e.ReceiverAccountId == account && (e.State & PostState.GoneForReceiver) == 0 &&
                                e.ExpiresAt > now);
    }

    /// <summary>
    /// A letter that carries nothing has nothing to take.
    /// </summary>
    private static PostState Nothing(long money, IReadOnlyList<PostItemPersistent> items)
    {
        return money == 0 && items.Count == 0 ? PostState.Received : PostState.None;
    }

    private async Task<PostPersistent?> ClaimAsync(IQueryable<PostPersistent> letters, CancellationToken cancellationToken)
    {
        if (await MarkAsync(letters.Where(e => (e.State & PostState.Received) == 0), PostState.Received | PostState.Read, cancellationToken) == 0)
        {
            return null;
        }

        return await letters.FirstOrDefaultAsync(cancellationToken);
    }

    private Task<int> MarkAsync(IQueryable<PostPersistent> letters, PostState state, CancellationToken cancellationToken)
    {
        var now = Now;

        return letters.ExecuteUpdateAsync(e => e
            .SetProperty(x => x.State, x => x.State | state)
            .SetProperty(x => x.UpdatedAt, now), cancellationToken);
    }

    /// <summary>
    /// Holds the row of the person until the transaction ends, so that nobody else adds a letter to its box meanwhile, and
    /// tells whether the box has room for one more. Letters whose time is up take no room.
    /// </summary>
    private static async Task<bool> HoldBoxAsync(PersistenceContext database, int person, DateTime now, CancellationToken cancellationToken)
    {
        await database.Database.ExecuteSqlAsync($"""SELECT 1 FROM "Persons" WHERE "Id" = {person} FOR UPDATE""", cancellationToken);

        return await Box(database.Posts, person, now).CountAsync(cancellationToken) < PostLimits.MaxLetters;
    }

    /// <summary>
    /// A letter neither side keeps goes. A letter of the game, or one whose sender is gone, has no side of the sender.
    /// </summary>
    private static Task<int> DropForgottenAsync(PersistenceContext database, long serial, CancellationToken cancellationToken)
    {
        return database.Posts
            .Where(e => e.Id == serial && (e.State & PostState.GoneForReceiver) != 0 &&
                        (e.SenderId == null || (e.State & PostState.GoneForSender) != 0))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
