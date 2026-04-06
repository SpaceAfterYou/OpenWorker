using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Ranking.Extensions;
using OpenWorker.Hotspot.Modules.Ranking.Types;

namespace OpenWorker.Hotspot.Modules.Ranking.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct RankingPvpListRequest(BinaryReader reader) : IRequestHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Ranking;
    private const RankingOpcode Command = RankingOpcode.PvpList;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public int Person { get; } = reader.ReadInt32();

    public RankingType Type { get; } = reader.ReadRankingType();

#endregion Message: Body
}
