using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Drops;

/// <summary>
/// Everything of a group is gone from the ground, nobody took it in time.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct DropDeleteResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Drop;
    private const DropOpcode Command = DropOpcode.Delete;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public required int DropGroup { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(DropGroup);
    }
}
