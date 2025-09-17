using System.Diagnostics;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Gestures.Types;

namespace OpenWorker.Hotspot.Modules.Gestures.Responses;

[HotspotMessage(Group, Command)]
public readonly struct GestureSlotUpdateResponse(IReadOnlyCollection<int> gestures) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Gesture;
    private const GestureOpcode Command = GestureOpcode.SlotUpdate;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        Debug.Assert(gestures.Count == GesturesModuleDefines.MaxGestureCount);
        
        foreach (var gesture in gestures)
        {
            writer.Write(gesture);
        }
    }
}