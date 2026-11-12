using OpenWorker.Hotspot.Modules.League.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.League.DataTypes;

[WireMessage]
public readonly partial struct LeagueInfoForMember
{
    public int League { get; init; }
    public LeaguePosition Position { get; init; }
    public long LeagueExp { get; init; }
    public long JoinDate { get; init; }
    public long ApplicationDate { get; init; }
}
