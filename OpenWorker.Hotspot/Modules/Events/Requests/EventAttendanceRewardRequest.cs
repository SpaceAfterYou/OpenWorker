using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Events.Requests;

[WireMessage]
[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly partial struct EventAttendanceRewardRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Event;
    private const EventOpcode Command = EventOpcode.AttendanceReward;

    public byte DayIndex { get; }
    
    public MessageOpcode Opcode => new(Group, Command);
}