using System.Diagnostics;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Gestures.Types;

namespace OpenWorker.Hotspot.Modules.Gestures.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct GestureSlotUpdateResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Gesture;
    private const GestureOpcode Command = GestureOpcode.SlotUpdate;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public required IReadOnlyCollection<int> Gestures { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        Debug.Assert(Gestures.Count == GesturesModuleDefines.MaxGestureCount);

        foreach (var gesture in Gestures)
        {
            writer.Write(gesture);
        }
    }

#endregion Interface: IWritableData
}
