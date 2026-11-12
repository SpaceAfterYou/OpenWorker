using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.League.DataTypes;

[WireMessage]
public readonly partial struct LeagueRewardInfo
{
    public int Auth { get; init; }
    public int LimitGoldOut { get; init; }
}
