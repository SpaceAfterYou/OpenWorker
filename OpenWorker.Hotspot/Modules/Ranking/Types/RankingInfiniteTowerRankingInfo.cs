using OpenWorker.Domain.Enums;

namespace OpenWorker.Hotspot.Modules.Ranking.Types;

public readonly record struct RankingInfiniteTowerRankingInfo
{
    public required int MyClearTime { get; init; }
    public required int Chapter { get; init; }
    public required int Stage { get; init; }
    public required int Person { get; init; }
    public required Hero Hero { get; init; }
    public required byte Level { get; init; }
    public required string Name { get; init; }
    public required int ClearTime { get; init; }
}