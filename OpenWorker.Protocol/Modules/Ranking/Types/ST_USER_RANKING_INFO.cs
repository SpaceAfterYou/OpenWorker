using OpenWorker.Domain.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Ranking.Types;

[WireMessage(WireMethods.Write)]
public readonly partial struct ST_USER_RANKING_INFO
{
    public required int Person { get; init; }
    public required short Order { get; init; }
    [WireAs(typeof(byte))]
    public required Hero Hero { get; init; }
    public required byte Level { get; init; }
    [WireString(WireStringFormat.Utf16Unicode)]
    public required string Name { get; init; }
    public required int KillScore { get; init; }
}