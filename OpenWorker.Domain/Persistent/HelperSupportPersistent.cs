namespace OpenWorker.Domain.Persistent;

/// <summary>
/// The orb a person offers its friends for their helpers, one per person: the kind of orb, what it adds, until when it can be
/// used, and whether the reward for offering it is still to be taken. A new orb can be offered once the day of the last one
/// is over and its reward was taken.
/// </summary>
public sealed class HelperSupportPersistent : BasicPersistent
{
    /// <summary>
    /// The reward for offering the orb is waiting to be taken.
    /// </summary>
    public const byte RewardWaiting = 1;

    /// <summary>
    /// The reward was taken.
    /// </summary>
    public const byte RewardTaken = 2;

    public int PersonId { get; init; }

    /// <summary>
    /// 0 for an orb of attack, 1 for an orb of defense.
    /// </summary>
    public byte SupportType { get; set; }

    public float Value { get; set; }

    public DateTime Until { get; set; }

    public byte Reward { get; set; }
}

/// <summary>
/// A friend put the orb of a person on one of its helpers. Each friend uses one orb once; the registration is told apart by
/// the end of its day.
/// </summary>
public sealed class HelperSupportUsePersistent : BasicPersistent
{
    public long Id { get; init; }

    public int SupporterId { get; init; }

    public int UserId { get; init; }

    /// <summary>
    /// The end of the day of the orb that was used.
    /// </summary>
    public DateTime Until { get; init; }
}
