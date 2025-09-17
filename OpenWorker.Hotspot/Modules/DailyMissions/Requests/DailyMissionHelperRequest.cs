using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.DailyMissions.Enums;
using OpenWorker.Hotspot.Modules.DailyMissions.Extensions;

namespace OpenWorker.Hotspot.Modules.DailyMissions.Requests;

[HotspotMessage(Group, Command)]
public readonly struct DailyMissionHelperRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.DailyMission;
    private const DailyMissionOpcode Command = DailyMissionOpcode.Helper;

    public int Mission { get; } = reader.ReadInt32();
    public DailyMissionType Type { get; } = reader.ReadDailyMissionType();

    public MessageOpcode Opcode => new(Group, Command);
}