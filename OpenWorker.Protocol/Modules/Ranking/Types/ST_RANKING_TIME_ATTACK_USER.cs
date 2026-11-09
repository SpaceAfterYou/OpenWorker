using OpenWorker.Domain.Enums;

namespace OpenWorker.Hotspot.Modules.Ranking.Types;

public readonly record struct ST_RANKING_TIME_ATTACK_USER
{
    public required int Person { get; init; }
    public required int World { get; init; }
    public required short Order { get; init; }
    public required Hero Hero { get; init; }
    public required byte Level { get; init; }
    public required string Name { get; init; }
    public required int Time { get; init; }
    public required int Score { get; init; }
}