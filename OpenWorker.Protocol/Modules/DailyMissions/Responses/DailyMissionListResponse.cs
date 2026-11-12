using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.DailyMissions.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.DailyMissions.Responses;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct DailyMissionListResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.DailyMission;
    private const DailyMissionOpcode Command = DailyMissionOpcode.List;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    /// <summary>
    /// The missions were drawn again for the day, which the client tells the person about.
    /// </summary>
    public bool TodayInit { get; init; }

    [WireArray(WireLength.Signed16)]
    public required IReadOnlyCollection<DailyMissionGroupValue> Groups { get; init; }

#endregion Message: Body
}
