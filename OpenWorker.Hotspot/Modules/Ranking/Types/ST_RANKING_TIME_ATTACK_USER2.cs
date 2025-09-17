namespace OpenWorker.Hotspot.Modules.Ranking.Types;

public readonly record struct ST_RANKING_TIME_ATTACK_USER2
{
    public required ST_RANKING_TIME_ATTACK_USER Player { get; init; }
    public required ST_RANKING_TIME_ATTACK_USER3 ParticipantList { get; init; }
}