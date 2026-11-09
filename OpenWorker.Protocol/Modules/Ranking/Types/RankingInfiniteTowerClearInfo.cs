using OpenWorker.Hotspot.Modules.Ranking.Responses;

namespace OpenWorker.Hotspot.Modules.Ranking.Types;

public readonly record struct RankingInfiniteTowerClearInfo
{
    /// <summary>
    /// <para>Length - 15</para>
    /// </summary>
    public required int[] FriendPassedPerFloor { get; init; }
    
    /// <summary>
    /// Me or friend.
    /// 
    /// <para>Length - 5</para>
    /// </summary>
    public required RankingInfiniteTowerRankingInfo[] BestOfStage { get; init; }
    
    public required int Chapter { get; init; }
}