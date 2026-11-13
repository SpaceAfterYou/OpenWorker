using System.Diagnostics;
using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.League.DataTypes;
using OpenWorker.Hotspot.Modules.Persons.Extensions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.League.Requests;

/// <summary>
/// Writes the notice of the league.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct LeagueNoticeChangeRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.NoticeChange;

    public int League { get; }
    [WireString(WireStringFormat.Utf16Unicode)]
    public string Notice { get; }
    public long Date { get; }
    public int Result { get; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
