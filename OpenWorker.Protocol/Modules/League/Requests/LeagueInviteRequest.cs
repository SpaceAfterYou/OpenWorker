using System.Diagnostics;
using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.League.DataTypes;
using OpenWorker.Hotspot.Modules.League.Extensions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.League.Requests;

/// <summary>
/// Asks a player to join the league.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct LeagueInviteRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.Invite;

    [WireString(WireStringFormat.Utf8Unicode)]
    public string LeagueName { get; }
    [WireString(WireStringFormat.Utf8Unicode)]
    public string TargetName { get; }
    [WireString(WireStringFormat.Utf8Unicode)]
    public string RequesterName { get; }
    public int Actor { get; }
    public int TargetActor { get; }
    public int League { get; }
    public int Result { get; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
