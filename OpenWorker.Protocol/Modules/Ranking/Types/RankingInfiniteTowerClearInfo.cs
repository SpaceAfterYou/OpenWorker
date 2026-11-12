using OpenWorker.Hotspot.Modules.Ranking.Responses;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Ranking.Types;

[WireMessage(WireMethods.Write)]
public readonly partial record struct RankingInfiniteTowerClearInfo
{
    /// <summary>
    /// For each of the fifteen chapters, how many friends of the person have cleared it.
    /// <para>Length - 15</para>
    /// </summary>
    [WireArray(Count = RankingInfiniteTowerListResponse.Chapters)]
    public required int[] FriendsClearedPerChapter { get; init; }

    /// <summary>
    /// The five stages of the chapter asked for.
    /// <para>Length - 5</para>
    /// </summary>
    [WireArray(Count = RankingInfiniteTowerListResponse.Stages)]
    public required RankingInfiniteTowerRankingInfo[] Stages { get; init; }

    public required int Chapter { get; init; }
}
