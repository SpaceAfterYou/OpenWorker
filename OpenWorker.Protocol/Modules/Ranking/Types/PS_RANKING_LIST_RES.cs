using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Ranking.Types;

[WireMessage(WireMethods.Write)]
public readonly partial struct PS_RANKING_LIST_RES
{
    public required RankingType Type { get; init; }
    public required ST_USER_RANKING_INFO Player { get; init; }
    [WireArray(WireLength.Signed16)]
    public required IReadOnlyCollection<ST_USER_RANKING_INFO> ParticipantList { get; init; }
}