using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.League.DataTypes;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.League.Responses;

/// <summary>
/// The leagues that were found.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct LeagueSearchResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.Search;

    public LeagueSearchResponse()
    {
    }

    public byte State { get; init; }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string Name { get; init; } = string.Empty;

    [WireString(WireStringFormat.Utf16Unicode)]
    public string Master { get; init; } = string.Empty;

    public int Reserved { get; init; }

    [WireArray(WireLength.Byte)]
    public IReadOnlyList<LeagueInfo> Leagues { get; init; } = [];

    [WireArray(WireLength.Byte)]
    public IReadOnlyList<int> Actors { get; init; } = [];

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
