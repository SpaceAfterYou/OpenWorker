using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.League.DataTypes;

public readonly struct LeagueMember
{
    public LeagueMember()
    {
        LeagueInfo = new LeagueInfoForMember();
        Login = false;
        World = 0;
        Channel = 0;
        Person = 0;
        Name = string.Empty;
        Level = 0;
        BoardLimitTime = 0;
        Hero = Hero.Haru;
        PlayDate = 0;
    }
    
    public LeagueMember(BinaryReader reader)
    {
        LeagueInfo = new LeagueInfoForMember(reader);
        Login = reader.ReadBoolean();
        World = reader.ReadInt16();
        Channel = reader.ReadByte();
        Person = reader.ReadInt32();
        Name = reader.ReadPersonName();
        Level = reader.ReadInt16();
        BoardLimitTime = reader.ReadUInt64();
        Hero = reader.ReadHero();
        PlayDate = reader.ReadUInt64();
    }

    public LeagueInfoForMember LeagueInfo { get; init; }
    public bool Login { get; init; }
    public short World { get; init; }
    public byte Channel { get; init; }
    public int Person { get; init; }
    public string Name { get; init; }
    public short Level { get; init; }
    public ulong BoardLimitTime { get; init; }
    public Hero Hero { get; init; }
    public ulong PlayDate { get; init; }
}