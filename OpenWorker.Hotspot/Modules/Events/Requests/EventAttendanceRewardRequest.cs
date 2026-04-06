using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Events.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct EventAttendanceRewardRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Event;
    private const EventOpcode Command = EventOpcode.AttendanceReward;

    public byte DayIndex { get; } = reader.ReadByte();
    
    public MessageOpcode Opcode => new(Group, Command);
}