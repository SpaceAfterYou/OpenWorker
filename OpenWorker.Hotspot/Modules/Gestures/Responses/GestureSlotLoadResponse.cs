using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Gestures.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct GestureSlotLoadResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Gesture;
    private const GestureOpcode Command = GestureOpcode.SlotLoad;

    public MessageOpcode Opcode => new(Group, Command);

    public required int[] Gestures { get; init; }

    public void Write(BinaryWriter writer)
    {
        foreach (var identifier in Gestures)
        {
            writer.Write(identifier);
        }
    }
}
