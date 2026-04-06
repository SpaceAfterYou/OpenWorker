using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Events.Requests;

/// <summary>
/// This packet doesn't have any content.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct EventAttendancePlayTimeRewardRequest(BinaryReader _) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Event;
    private const EventOpcode Command = EventOpcode.AttendancePlayTimeReward;

    public MessageOpcode Opcode => new(Group, Command);
}