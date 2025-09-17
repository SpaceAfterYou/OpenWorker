namespace OpenWorker.Hotspot.Modules.Ranking.Types;

public readonly record struct ST_RANKING_TIME_ATTACK_USER3
{
    public required int World { get; init; }
    public required ST_RANKING_TIME_ATTACK_USER[] aUserList { get; init; }
}