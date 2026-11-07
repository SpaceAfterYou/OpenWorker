using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Drops;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct DropPickUpRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Drop;
    private const DropOpcode Command = DropOpcode.PickUp;

    public MessageOpcode Opcode => new(Group, Command);

    public int DropGroup { get; } = reader.ReadInt32();
    public long Serial { get; } = reader.ReadInt64();
}
