using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.League.DataTypes;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.League.Requests;

/// <summary>
/// Looks for a league by its name or its master.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct LeagueSearchRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.Search;

    public byte State { get; }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string Name { get; }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string Master { get; }

    public int Reserved { get; }

    [WireArray(WireLength.Byte)]
    public IReadOnlyList<LeagueInfo> Leagues { get; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
