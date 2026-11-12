using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.League.Responses;

/// <summary>
/// The answer to an invitation you sent, the number of the text in its result.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct LeagueInviteResResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.InviteRes;

    public LeagueInviteResResponse()
    {
    }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string LeagueName { get; init; } = string.Empty;

    [WireString(WireStringFormat.Utf16Unicode)]
    public string TargetName { get; init; } = string.Empty;

    [WireString(WireStringFormat.Utf16Unicode)]
    public string RequesterName { get; init; } = string.Empty;

    public int Actor { get; init; }
    public int TargetActor { get; init; }
    public int League { get; init; }
    public int Result { get; init; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
