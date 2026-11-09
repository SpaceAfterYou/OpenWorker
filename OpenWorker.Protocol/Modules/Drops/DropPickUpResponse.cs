using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Drops;

/// <summary>
/// The thing is taken, the client removes it from the ground.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct DropPickUpResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Drop;
    private const DropOpcode Command = DropOpcode.PickUp;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public required long Serial { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Serial);
    }
}
