namespace OpenWorker.Hotspot.Modules.Ranking.Types;

public readonly record struct RankingInfiniteTowerClearInfo
{
    /// <summary>
    /// For each of the fifteen chapters, how many friends of the person have cleared it.
    /// <para>Length - 15</para>
    /// </summary>
    public required int[] FriendsClearedPerChapter { get; init; }

    /// <summary>
    /// The five stages of the chapter asked for.
    /// <para>Length - 5</para>
    /// </summary>
    public required RankingInfiniteTowerRankingInfo[] Stages { get; init; }

    public required int Chapter { get; init; }
}
