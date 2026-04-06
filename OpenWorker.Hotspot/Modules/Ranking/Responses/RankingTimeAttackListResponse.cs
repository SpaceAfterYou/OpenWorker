using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;
using OpenWorker.Hotspot.Modules.Ranking.Types;

namespace OpenWorker.Hotspot.Modules.Ranking.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct RankingTimeAttackListResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Ranking;
    private const RankingOpcode Command = RankingOpcode.TimeAttackList;

    public MessageOpcode Opcode => new(Group, Command);

    public ST_RANKING_TIME_ATTACK_USER2 Data { get; init; }

    public void Write(BinaryWriter writer)
    {
        Write(writer, Data);
    }

    private static void Write(BinaryWriter writer, ST_RANKING_TIME_ATTACK_USER2 data)
    {
        WriteUserRankingInfo(writer, data.Player);
        Write(writer, data.ParticipantList);
    }

    private static void Write(BinaryWriter writer, ST_RANKING_TIME_ATTACK_USER3 data)
    {
        writer.Write(data.World);
        WriteUserRankingInfos(writer, data.aUserList);
    }

    private static void WriteUserRankingInfos(BinaryWriter writer, IReadOnlyCollection<ST_RANKING_TIME_ATTACK_USER> infos)
    {
        writer.Write((short)infos.Count);

        foreach (var info in infos)
        {
            WriteUserRankingInfo(writer, info);
        }
    }

    private static void WriteUserRankingInfo(BinaryWriter writer, ST_RANKING_TIME_ATTACK_USER info)
    {
        writer.Write(info.Person);
        writer.Write(info.World);
        writer.Write(info.Order);
        writer.Write(info.Hero);
        writer.Write(info.Level);
        writer.WriteUtf16UnicodeString(info.Name, 21);
        writer.Write(info.Time);
        writer.Write(info.Score);
    }
}
