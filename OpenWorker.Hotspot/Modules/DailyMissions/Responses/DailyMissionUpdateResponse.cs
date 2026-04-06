using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.DailyMissions.Extensions;
using OpenWorker.Hotspot.Modules.DailyMissions.Types;

namespace OpenWorker.Hotspot.Modules.DailyMissions.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct DailyMissionUpdateResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.DailyMission;
    private const DailyMissionOpcode Command = DailyMissionOpcode.Update;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public required IReadOnlyList<DailyMissionValue> List { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(List);
    }

#endregion Interface: IWritableData
}
