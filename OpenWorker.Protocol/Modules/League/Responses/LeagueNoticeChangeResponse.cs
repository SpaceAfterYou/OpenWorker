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

namespace OpenWorker.Hotspot.Modules.League.Responses;

/// <summary>
/// The notice of the league changed.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct LeagueNoticeChangeResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.NoticeChange;

    public int League { get; init; }
    public string Notice { get; init; } = string.Empty;
    public long Date { get; init; }
    public int Result { get; init; }

    public LeagueNoticeChangeResponse()
    {
    }

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public void Write(BinaryWriter writer)
    {
        writer.Write(League);
        writer.WriteUtf16UnicodeString(Notice, 801);
        writer.Write(Date);
        writer.Write(Result);
    }
}
