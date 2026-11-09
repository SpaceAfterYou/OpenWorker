using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.DailyMissions.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.DailyMissions.Requests;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct DailyMissionHelperRequest : IRequestHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.DailyMission;
    private const DailyMissionOpcode Command = DailyMissionOpcode.Helper;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public int Mission { get; }

    /// <summary>
    /// Whether the mission is to be shown in the tracker.
    /// </summary>
    public DailyMissionHelperState Helper { get; }

#endregion Message: Body
}
