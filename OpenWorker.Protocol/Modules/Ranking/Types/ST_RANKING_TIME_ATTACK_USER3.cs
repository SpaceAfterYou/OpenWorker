using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Ranking.Types;

[WireMessage(WireMethods.Write)]
public readonly partial record struct ST_RANKING_TIME_ATTACK_USER3
{
    public required int World { get; init; }
    [WireArray(WireLength.Signed16)]
    public required ST_RANKING_TIME_ATTACK_USER[] aUserList { get; init; }
}