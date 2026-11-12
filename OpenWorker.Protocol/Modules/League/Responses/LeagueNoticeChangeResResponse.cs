using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.League.Responses;

/// <summary>
/// The answer to writing the notice.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct LeagueNoticeChangeResResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.NoticeChangeRes;

    public LeagueNoticeChangeResResponse()
    {
    }

    public int League { get; init; }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string Notice { get; init; } = string.Empty;

    public long Date { get; init; }
    public int Result { get; init; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
