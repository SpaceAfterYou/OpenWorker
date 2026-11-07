using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.World.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct WorldCheckSectorRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.World;
    private const WorldOpcode Command = WorldOpcode.CheckSectorReq;

    public int Location { get; } = reader.ReadInt32();
    public int Box { get; } = reader.ReadInt32();

    public MessageOpcode Opcode => new(Group, Command);
}
