using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Ranking.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Ranking.Responses;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct RankingTimeAttackListResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Ranking;
    private const RankingOpcode Command = RankingOpcode.TimeAttackList;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public ST_RANKING_TIME_ATTACK_USER2 Data { get; init; }
}
