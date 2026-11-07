using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.DailyMissions.Enums;

namespace OpenWorker.Hotspot.Modules.DailyMissions.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct DailyMissionHelperRequest(BinaryReader reader) : IRequestHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.DailyMission;
    private const DailyMissionOpcode Command = DailyMissionOpcode.Helper;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public int Mission { get; } = reader.ReadInt32();

    /// <summary>
    /// Whether the mission is to be shown in the tracker.
    /// </summary>
    public DailyMissionHelperState Helper { get; } = (DailyMissionHelperState)reader.ReadByte();

#endregion Message: Body
}
