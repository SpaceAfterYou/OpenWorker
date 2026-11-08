namespace OpenWorker.Domain.Persistent;

/// <summary>
/// How a person stands to another in their lists. Everybody has their own side of a friendship, so a request or a
/// friendship takes two rows, one for each person.
/// </summary>
public enum FriendKind : byte
{
    Friend = 1,

    /// <summary>
    /// The other one asked, and the owner has to answer.
    /// </summary>
    Wait = 2,

    /// <summary>
    /// The owner asked, and the other one has not answered yet.
    /// </summary>
    Invite = 3,

    Block = 101
}

public sealed class FriendPersistent : BasicPersistent
{
    public int Id { get; init; }

    public required int OwnerId { get; init; }

    public required int TargetId { get; init; }

    public required FriendKind Kind { get; set; }

    /// <summary>
    /// What the two have earned together as friends.
    /// </summary>
    public long Point { get; set; }
}
