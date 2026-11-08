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

namespace OpenWorker.Hotspot.Modules.League.Requests;

/// <summary>
/// Writes the notice of the league.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct LeagueNoticeChangeRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.NoticeChange;

    public int League { get; } = reader.ReadInt32();
    public string Notice { get; } = reader.ReadUtf8UnicodeString(801);
    public long Date { get; } = reader.ReadInt64();
    public int Result { get; } = reader.ReadInt32();

    public MessageOpcode Opcode => new(Group, Command);
}
