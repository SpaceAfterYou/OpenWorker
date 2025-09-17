using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Gestures.Types;

namespace OpenWorker.Hotspot.Modules.Gestures.Request;

[HotspotMessage(Group, Command)]
public readonly struct GestureSlotUpdateRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Gesture;
    private const GestureOpcode Command = GestureOpcode.SlotUpdate;

    public MessageOpcode Opcode => new(Group, Command);

    public int[] GestureList { get; } = reader.ReadInt32AsArray(GesturesModuleDefines.MaxGestureCount);
}