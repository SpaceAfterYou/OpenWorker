using OpenWorker.Domain.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.League.DataTypes;

[WireMessage(WireMethods.Write)]
public readonly partial struct LeagueMember
{
    public LeagueMember()
    {
        Name = string.Empty;
        Hero = Hero.Haru;
    }

    public LeagueInfoForMember LeagueInfo { get; init; }
    public bool Login { get; init; }
    public short World { get; init; }
    public byte Channel { get; init; }
    public int Person { get; init; }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string Name { get; init; }

    public short Level { get; init; }
    public ulong BoardLimitTime { get; init; }

    [WireAs(typeof(byte))]
    public Hero Hero { get; init; }

    public ulong PlayDate { get; init; }
}
