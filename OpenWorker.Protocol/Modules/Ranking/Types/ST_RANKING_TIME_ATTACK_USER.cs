using OpenWorker.Domain.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Ranking.Types;

[WireMessage(WireMethods.Write)]
public readonly partial record struct ST_RANKING_TIME_ATTACK_USER
{
    public required int Person { get; init; }
    public required int World { get; init; }
    public required short Order { get; init; }
    [WireAs(typeof(byte))]
    public required Hero Hero { get; init; }
    public required byte Level { get; init; }
    [WireString(WireStringFormat.Utf16Unicode)]
    public required string Name { get; init; }
    public required int Time { get; init; }
    public required int Score { get; init; }
}