using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Ranking.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct RankingInfiniteTowerListRequest(BinaryReader reader) : IRequestHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Ranking;
    private const RankingOpcode Command = RankingOpcode.InfiniteTowerList;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public int Person { get; } = reader.ReadInt32();
    public int Chapter { get; } = reader.ReadInt32();

#endregion Message: Body
}
