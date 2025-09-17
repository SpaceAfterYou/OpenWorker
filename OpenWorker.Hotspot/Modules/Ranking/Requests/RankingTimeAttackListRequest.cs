using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Ranking.Requests;

[HotspotMessage(Group, Command)]
public readonly struct RankingTimeAttackListRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Ranking;
    private const RankingOpcode Command = RankingOpcode.TimeAttackList;
    
    public int Person { get; } = reader.ReadInt32();
    
    public int World { get; } = reader.ReadInt32();
    
    public MessageOpcode Opcode => new(Group, Command);
}