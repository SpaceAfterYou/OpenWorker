using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person.Components;
using OpenWorker.Hotspot.Modules.Persons.Extensions;
using OpenWorker.Hotspot.Modules.Ranking.Extensions;
using OpenWorker.Hotspot.Modules.Ranking.Types;

namespace OpenWorker.Hotspot.Modules.Ranking.Responses;

[HotspotMessage(Group, Command)]
public readonly struct RankingPvpListResponse(Arch.Core.World world, Entity player, RankingType type) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Ranking;
    private const RankingOpcode Command = RankingOpcode.PvpList;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        var data = new PS_RANKING_LIST_RES
        {
            Type = type,
            Player = new ST_USER_RANKING_INFO
            {
                Person = world.Get<ActorComponent>(player).Identifier,
                Order = 1,
                Hero = world.Get<PersonInfoComponent>(player).Hero,
                Level = (byte)Random.Shared.Next(1, byte.MaxValue),
                Name = world.Get<PersonInfoComponent>(player).Name,
                KillScore = (int)Random.Shared.Next(1, short.MaxValue)
            },
            ParticipantList = CreateRandomUserRankingInfo().ToArray()
        };
        
        Write(writer, data);
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
    
    private static void Write(BinaryWriter writer, PS_RANKING_LIST_RES data)
    {
        writer.Write(data.Type);
        
        WriteUserRankingInfo(writer, data.Player);
        WriteUserRankingInfos(writer, data.ParticipantList);
    }
    
    private static IEnumerable<ST_USER_RANKING_INFO> CreateRandomUserRankingInfo()
    {
        for (short i = 0; i < 3; i++)
        {
            yield return new ST_USER_RANKING_INFO
            {
                Person = (int)(10_000 + i),
                Order = (short)(1 + i),
                Hero = (Hero)Random.Shared.Next(1, 7),
                Level = (byte)Random.Shared.Next(1, byte.MaxValue),
                Name = $"cool girl {1 + i}",
                KillScore = (int)Random.Shared.Next(1, short.MaxValue)
            };
        }
    }
}