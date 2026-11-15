using OpenWorker.Domain.Persistent;

namespace OpenWorker.Domain.Stores;

/// <summary>
/// A letter a person writes: to whom (by name), what, and what goes with it.
/// </summary>
public sealed record PostDraft(
    int Sender,
    string SenderName,
    byte SenderHero,
    string ReceiverName,
    string Title,
    string Body,
    long Money,
    long Postage,
    IReadOnlyList<PostItemPersistent> Items);

/// <summary>
/// A letter of the game or a game master for an account.
/// </summary>
public sealed record PostAccountDraft(
    int Account,
    string SenderName,
    string Title,
    string Body,
    long Money,
    IReadOnlyList<PostItemPersistent> Items,
    byte MainType = 0,
    byte SubType = 0)
{
    /// <summary>
    /// The serial the letter is written under (<see cref="IPostStore.ReserveSerialAsync"/>), or 0 for a new one. A letter
    /// written again under its serial is written once: the store gives back the letter already there.
    /// </summary>
    public long Serial { get; init; }
}

public enum PostSendOutcome
{
    Sent,

    /// <summary>
    /// Nobody has that name, or the receiver does not take letters from the sender.
    /// </summary>
    NoReceiver,

    /// <summary>
    /// The sender wrote to themselves.
    /// </summary>
    Self,

    /// <summary>
    /// The box of the receiver holds as many letters as it can.
    /// </summary>
    Full
}

/// <param name="Letter">The letter as it was stored, when it was.</param>
/// <param name="Unread">The letters of the receiver not yet read, this one included.</param>
public readonly record struct PostSendResult(PostSendOutcome Outcome, PostPersistent? Letter, int Unread);

public enum PostDeleteOutcome
{
    Deleted,

    /// <summary>
    /// The letter is not there, or not in the box of the one who asks.
    /// </summary>
    Missing,

    /// <summary>
    /// The money or the items of the letter have not been taken: it is not deleted with them.
    /// </summary>
    HasAttachments
}

public enum PostReturnOutcome
{
    Returned,
    Missing,

    /// <summary>
    /// A letter of the game has nobody to go back to.
    /// </summary>
    FromTheGame,

    /// <summary>
    /// A letter that came back once does not go back again.
    /// </summary>
    AlreadyReturned,

    /// <summary>
    /// The box of the first sender holds as many letters as it can.
    /// </summary>
    Full
}

/// <param name="Letter">The letter as it lies with its first sender now, when it went back.</param>
/// <param name="Unread">The letters of the first sender not yet read, this one included.</param>
public readonly record struct PostReturnResult(PostReturnOutcome Outcome, PostPersistent? Letter, int Unread);

public static class PostLimits
{
    /// <summary>
    /// The most letters one list shows, the newest, and the largest box of a person, kept letters included, so that every
    /// letter of a box is listed.
    /// </summary>
    public const int MaxLetters = 50;

    /// <summary>
    /// How long a letter is kept, from its sending or its sending back, whoever sent it. Then it is gone for both sides with
    /// what it still carries; a letter the receiver moved to the kept ones stays with the receiver.
    /// </summary>
    public static TimeSpan Lifetime { get; } = TimeSpan.FromDays(30);
}

/// <summary>
/// The letters of the game. The game writes a letter when it changes, at once: sending, reading, taking what it carries
/// and deleting are rare next to the changes the write-behind of a person collects, and the receiver may be on another
/// server, so the letter cannot wait for the flush of anybody. What the letter carries moves into an inventory through
/// the game, which marks it for that flush.
/// </summary>
public interface IPostStore
{
    /// <summary>
    /// Finds the receiver and stores the letter for them, in one transaction that holds the box of the receiver, so two
    /// servers that send at the same moment cannot overfill it. When the database took the letter but its answer was lost,
    /// this still says it was sent; when it throws, the letter is not there and never will be, unless the database could
    /// not be asked at all.
    /// </summary>
    Task<PostSendResult> SendAsync(PostDraft draft, CancellationToken cancellationToken = default);

    /// <summary>
    /// A serial for a letter of the game that is written under it (<see cref="PostAccountDraft.Serial"/>), so that its
    /// writes after a failure land once.
    /// </summary>
    Task<long> ReserveSerialAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a letter of the game for an account. Under a serial that is taken already, the letter there comes back.
    /// </summary>
    Task<PostPersistent> SendToAccountAsync(PostAccountDraft draft, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a letter of the game for an account that nobody sees yet (<see cref="PostState.Pending"/>): what it carries is
    /// each item and its count, made when it is delivered. Under a serial that is taken already, the letter there comes
    /// back, pending or delivered.
    /// </summary>
    Task<PostPersistent> SendPendingToAccountAsync(PostAccountDraft draft, CancellationToken cancellationToken = default);

    /// <summary>
    /// The pending letters written before a moment, oldest first.
    /// </summary>
    Task<List<PostPersistent>> ListPendingAsync(DateTime writtenBefore, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces a pending letter with the letter itself, under the same serial, with the items made for it and a lifetime
    /// from now, once: the letter comes back to the call that delivered it and <see langword="null"/> to every other, from any
    /// server.
    /// </summary>
    Task<PostPersistent?> DeliverPendingAsync(long serial, IReadOnlyList<PostItemPersistent> items, CancellationToken cancellationToken = default);

    /// <summary>
    /// The letters a person received, the kept ones or the others, newest first.
    /// </summary>
    Task<List<PostPersistent>> ListReceivedAsync(int person, bool saved, CancellationToken cancellationToken = default);

    /// <summary>
    /// The letters a person sent and still lists, newest first.
    /// </summary>
    Task<List<PostPersistent>> ListSentAsync(int person, CancellationToken cancellationToken = default);

    Task<List<PostPersistent>> ListAccountAsync(int account, CancellationToken cancellationToken = default);

    /// <summary>
    /// The letters not yet read: of the person, and of its account.
    /// </summary>
    Task<(int Person, int Account)> CountUnreadAsync(int person, int account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a letter of the person read and gives it back, or <see langword="null"/> when the person has no such letter.
    /// </summary>
    Task<PostPersistent?> ReadAsync(long serial, int person, CancellationToken cancellationToken = default);

    Task<PostPersistent?> ReadAccountAsync(long serial, int account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Takes what a letter of the person carries, once: the letter comes back to the call that took it and to no other,
    /// and <see langword="null"/> to every later call, from any server.
    /// </summary>
    Task<PostPersistent?> ClaimAsync(long serial, int person, CancellationToken cancellationToken = default);

    Task<PostPersistent?> ClaimAccountAsync(long serial, int account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Puts back what a claim took, when the game could not give it.
    /// </summary>
    Task ReleaseAsync(long serial, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a received letter to the kept ones.
    /// </summary>
    Task<PostPersistent?> SaveAsync(long serial, int person, CancellationToken cancellationToken = default);

    Task<PostDeleteOutcome> DeleteReceivedAsync(long serial, int person, CancellationToken cancellationToken = default);

    Task<PostDeleteOutcome> DeleteSentAsync(long serial, int person, CancellationToken cancellationToken = default);

    Task<PostDeleteOutcome> DeleteAccountAsync(long serial, int account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a received letter back to its sender, with what it still carries.
    /// </summary>
    Task<PostReturnResult> SendBackAsync(long serial, int person, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes at most <paramref name="limit"/> letters whose time is up, with what they still carry, and tells how many
    /// went. A letter kept by its receiver, or pending, is not one of them. Every list, count and change already passes over such a
    /// letter, so the sweep only frees the table; two servers may sweep at the same moment, and each letter goes once.
    /// </summary>
    Task<int> SweepAsync(int limit, CancellationToken cancellationToken = default);
}
