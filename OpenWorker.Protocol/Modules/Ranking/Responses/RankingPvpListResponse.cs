using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Ranking.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Ranking.Responses;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct RankingPvpListResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Ranking;
    private const RankingOpcode Command = RankingOpcode.PvpList;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public required PS_RANKING_LIST_RES Data { get; init; }
}
