using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.DailyMissions.Types;

namespace OpenWorker.Hotspot.Modules.DailyMissions.Responses;

[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct DailyMissionListResponse : IResponseHotspotMessage
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

    public required IReadOnlyCollection<DailyMissionGroupValue> Groups { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(TodayInit);
        writer.Write(checked((short)Groups.Count));

        foreach (var group in Groups)
        {
            group.Write(writer);
        }
    }

#endregion Interface: IWritableData
}
