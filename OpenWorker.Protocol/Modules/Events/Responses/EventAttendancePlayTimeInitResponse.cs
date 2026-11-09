using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Events.Responses;

/// <summary>
/// The time played today starts again, which the client does by itself. This message has no content.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct EventAttendancePlayTimeInitResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Event;
    private const EventOpcode Command = EventOpcode.AttendancePlayTimeInit;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage
}
