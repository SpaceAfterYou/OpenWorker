using OpenWorker.Hotspot.Modules.League.Enums;
using OpenWorker.Hotspot.Modules.League.Extensions;

namespace OpenWorker.Hotspot.Modules.League.DataTypes;

public readonly struct LeagueInfoForMember
{
    public LeagueInfoForMember()
    {
        League = 0;
        Position = LeaguePosition.BaseClass;
        LeagueExp = 0;
        JoinDate = 0;
        ApplicationDate = 0;    
    }
    
    public LeagueInfoForMember(BinaryReader reader)
    {
        League = reader.ReadInt32();
        Position = reader.ReadLeaguePosition();
        LeagueExp = reader.ReadInt64();
        JoinDate = reader.ReadInt64();
        ApplicationDate = reader.ReadInt64();
    }

    public int League { get; init; }
    public LeaguePosition Position { get; init; }
    public long LeagueExp { get; init; }
    public long JoinDate { get; init; }
    public long ApplicationDate { get; init; }
}