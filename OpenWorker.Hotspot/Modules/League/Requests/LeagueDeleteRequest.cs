using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.League.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct LeagueDeleteRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.Delete;

    public int Npc { get; } = reader.ReadInt32();
    
    public MessageOpcode Opcode => new(Group, Command);
}