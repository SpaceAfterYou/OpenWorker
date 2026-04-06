using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.DailyMissions.Enums;
using OpenWorker.Hotspot.Modules.DailyMissions.Extensions;

namespace OpenWorker.Hotspot.Modules.DailyMissions.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct DailyMissionAcceptResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.DailyMission;
    private const DailyMissionOpcode Command = DailyMissionOpcode.Accept;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public int Mission { get; init; }
    public DailyMissionState State { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Mission);
        writer.Write(State);
    }

#endregion Interface: IWritableData
}
