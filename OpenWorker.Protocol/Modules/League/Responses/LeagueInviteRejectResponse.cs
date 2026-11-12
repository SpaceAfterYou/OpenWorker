using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.League.Responses;

/// <summary>
/// The one you asked turned you down.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct LeagueInviteRejectResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.InviteReject;

    public LeagueInviteRejectResponse()
    {
    }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string TargetName { get; init; } = string.Empty;

    public int Requester { get; init; }
    public int Target { get; init; }
    public int Result { get; init; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
