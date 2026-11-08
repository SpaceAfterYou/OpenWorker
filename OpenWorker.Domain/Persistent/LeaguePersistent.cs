namespace OpenWorker.Domain.Persistent;

public sealed class LeaguePersistent : BasicPersistent
{
    public const int PositionNameCount = 3;
    public const int AuthCount = 9;

    public int Id { get; init; }

    public required string Name { get; set; }

    public required int MasterId { get; set; }

    public short CardEmblem { get; set; }
    public short CardBorder { get; set; }

    /// <summary>
    /// The level of the league, it gives the room for members.
    /// </summary>
    public byte Rating { get; set; } = 1;

    public long Exp { get; set; }
    public long Money { get; set; }

    /// <summary>
    /// Whether people may apply without being asked.
    /// </summary>
    public bool Open { get; set; } = true;

    public string Notice { get; set; } = string.Empty;
    public DateTime? NoticeAt { get; set; }

    public string RecruitNotice { get; set; } = string.Empty;
    public DateTime? RecruitNoticeAt { get; set; }

    /// <summary>
    /// What the first three positions are called in this league.
    /// </summary>
    public string[] PositionNames { get; set; } = new string[PositionNameCount].Select(_ => string.Empty).ToArray();

    /// <summary>
    /// What a position may do, as bits, and how much gold it may take out; a slot for each position.
    /// </summary>
    public int[] Auth { get; set; } = new int[AuthCount];
    public int[] LimitGoldOut { get; set; } = new int[AuthCount];
}
