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
/// Gives a position to a member or takes it away.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct LeagueMemberPositionChangeRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.MemberPositionChange;

    public int Actor { get; } = reader.ReadInt32();
    public byte Position { get; } = reader.ReadByte();
    public byte State { get; } = reader.ReadByte();
    public int Result { get; } = reader.ReadInt32();

    public MessageOpcode Opcode => new(Group, Command);
}
