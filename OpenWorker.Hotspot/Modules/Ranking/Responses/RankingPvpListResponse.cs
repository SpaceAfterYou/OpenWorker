using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;
using OpenWorker.Hotspot.Modules.Ranking.Extensions;
using OpenWorker.Hotspot.Modules.Ranking.Types;

namespace OpenWorker.Hotspot.Modules.Ranking.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct RankingPvpListResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Ranking;
    private const RankingOpcode Command = RankingOpcode.PvpList;

    public MessageOpcode Opcode => new(Group, Command);

    public required PS_RANKING_LIST_RES Data { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Data.Type);

        WriteUserRankingInfo(writer, Data.Player);
        WriteUserRankingInfos(writer, Data.ParticipantList);
    }

    private static void WriteUserRankingInfo(BinaryWriter writer, ST_USER_RANKING_INFO info)
    {
        writer.Write(info.Person);
        writer.Write(info.Order);
        writer.Write(info.Hero);
        writer.Write(info.Level);
        writer.WritePersonName(info.Name);
        writer.Write(info.KillScore);
    }

    private static void WriteUserRankingInfos(BinaryWriter writer, IReadOnlyCollection<ST_USER_RANKING_INFO> infos)
    {
        writer.Write((short)infos.Count);

        foreach (var info in infos)
        {
            WriteUserRankingInfo(writer, info);
        }
    }
}
