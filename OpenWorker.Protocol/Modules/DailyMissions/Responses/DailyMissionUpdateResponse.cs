using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.DailyMissions.Types;

namespace OpenWorker.Hotspot.Modules.DailyMissions.Responses;

[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct DailyMissionUpdateResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.DailyMission;
    private const DailyMissionOpcode Command = DailyMissionOpcode.Update;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public required IReadOnlyCollection<DailyMissionValue> List { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(checked((short)List.Count));

        foreach (var mission in List)
        {
            mission.Write(writer);
        }
    }

#endregion Interface: IWritableData
}
