namespace OpenWorker.Hotspot.Modules.Ranking.Types;

struct PS_RANKING_LIST_RES
{
    public required RankingType Type { get; init; }
    public required ST_USER_RANKING_INFO Player { get; init; }
    public required IReadOnlyCollection<ST_USER_RANKING_INFO> ParticipantList { get; init; }
}