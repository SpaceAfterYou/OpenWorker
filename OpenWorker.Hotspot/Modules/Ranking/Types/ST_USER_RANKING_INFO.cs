using OpenWorker.Domain.Enums;

namespace OpenWorker.Hotspot.Modules.Ranking.Types;

struct ST_USER_RANKING_INFO
{
    public required int Person { get; init; }
    public required short Order { get; init; }
    public required Hero Hero { get; init; }
    public required byte Level { get; init; }
    public required string Name { get; init; }
    public required int KillScore { get; init; }
}