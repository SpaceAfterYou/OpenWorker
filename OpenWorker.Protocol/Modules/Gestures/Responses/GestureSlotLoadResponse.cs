using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Gestures.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Gestures.Responses;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct GestureSlotLoadResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Gesture;
    private const GestureOpcode Command = GestureOpcode.SlotLoad;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    /// <summary>
    /// The gestures of the six slots: the client reads that many without a count.
    /// </summary>
    [WireArray(Count = GesturesModuleDefines.MaxGestureCount)]
    public required int[] Gestures { get; init; }
}
