using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person.Components;
using OpenWorker.Hotspot.Modules.Persons.Extensions;
using OpenWorker.Hotspot.Modules.Ranking.Types;

namespace OpenWorker.Hotspot.Modules.Ranking.Responses;

[HotspotMessage(Group, Command)]
public readonly record struct RankingTimeAttackListResponse(ST_RANKING_TIME_ATTACK_USER2 Data) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Ranking;
    private const RankingOpcode Command = RankingOpcode.TimeAttackList;

    public MessageOpcode Opcode => new(Group, Command);

    public static RankingTimeAttackListResponse Create(Arch.Core.World world, Entity player, int targetWorld)
    {
        var data = new ST_RANKING_TIME_ATTACK_USER2
        {
            Player = new ST_RANKING_TIME_ATTACK_USER
            {
                Person = world.Get<ActorComponent>(player).Identifier,
                World = targetWorld,
                Order = 22,
                Hero = world.Get<PersonInfoComponent>(player).Hero,
                Level = 1,
                Name = world.Get<PersonInfoComponent>(player).Name,
                Time = 1_000,
                Score = 10_000
            },
            ParticipantList = new ST_RANKING_TIME_ATTACK_USER3
            {
                World = targetWorld,
                aUserList = Enumerable.Range(0, 50).Select((x, i) => new ST_RANKING_TIME_ATTACK_USER
                {
                    Person = (int)(10_000 + i),
                    World = targetWorld,
                    Order = (short)(1 + i),
                    Hero = (Hero)Random.Shared.Next(1, 7),
                    Level = (byte)Random.Shared.Next(1, byte.MaxValue),
                    Name = $"rank. test name {i}",
                    Time = 10 + i,
                    Score = 100 + i
                }).ToArray()
            }
        };

        return new RankingTimeAttackListResponse(data);
    }
    
    public void ToBinary(BinaryWriter writer)
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