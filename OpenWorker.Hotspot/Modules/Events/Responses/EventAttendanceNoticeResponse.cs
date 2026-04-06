using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Events.Extensions;
using OpenWorker.Hotspot.Modules.Events.Types;

namespace OpenWorker.Hotspot.Modules.Events.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct EventAttendanceNoticeResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Event;
    private const EventOpcode Command = EventOpcode.AttendanceNotice;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public AttendanceInfo Info { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Info);
    }

#endregion Interface: IWritableData
}
