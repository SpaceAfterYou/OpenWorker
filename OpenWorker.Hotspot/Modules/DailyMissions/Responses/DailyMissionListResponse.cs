using System.Linq;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.DailyMissions.Extensions;
using OpenWorker.Hotspot.Modules.DailyMissions.Types;

namespace OpenWorker.Hotspot.Modules.DailyMissions.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct DailyMissionListResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.DailyMission;
    private const DailyMissionOpcode Command = DailyMissionOpcode.List;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public bool TodayInit { get; init; }
    public required IReadOnlyList<DailyMissionValue> List { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(TodayInit);
        writer.Write(List.Take(1).ToArray());
    }

#endregion Interface: IWritableData
}
