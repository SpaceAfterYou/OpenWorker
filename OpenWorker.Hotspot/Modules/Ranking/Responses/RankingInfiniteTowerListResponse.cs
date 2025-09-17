using System.Diagnostics;
using OpenWorker.Domain.Enums;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;
using OpenWorker.Hotspot.Modules.Ranking.Types;

namespace OpenWorker.Hotspot.Modules.Ranking.Responses;

[HotspotMessage(Group, Command)]
public readonly record struct RankingInfiniteTowerListResponse(RankingInfiniteTowerClearInfo Data) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Ranking;
    private const RankingOpcode Command = RankingOpcode.InfiniteTowerList;

    public MessageOpcode Opcode => new(Group, Command);

    public static RankingInfiniteTowerListResponse Create(int chapter)
    {
        var data = new RankingInfiniteTowerClearInfo
        {
            Chapter = chapter,
            FriendPassedPerFloor = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15],
            BestOfStage = Enumerable
                .Range(0, 5)
                .Select(x => new RankingInfiniteTowerRankingInfo
                {
                    MyClearTime = 100 * (1 + x), 
                    Chapter = chapter, 
                    Stage = 1 + x,
                    Person = 10_000 + x,
                    Hero = (Hero)Random.Shared.Next(1, 7),
                    Level = (byte)Random.Shared.Next(1, byte.MaxValue),
                    Name = $"cool girl {1 + x}",
                    ClearTime = Random.Shared.Next(1_000, 10_000)
                })
                .ToArray()
        };

        return new RankingInfiniteTowerListResponse(data);
    }
    
    public void ToBinary(BinaryWriter writer)
    {
        Debug.Assert(Data.FriendPassedPerFloor.Length == 15);
        
        foreach (var info in Data.FriendPassedPerFloor)
        {
            writer.Write(info);
        }
        
        Debug.Assert(Data.BestOfStage.Length == 5);
        
        foreach (var friend in Data.BestOfStage)
        {
            writer.Write(friend.MyClearTime);
            writer.Write(friend.Chapter);
            writer.Write(friend.Stage);
            writer.Write(friend.Person);
            writer.Write(friend.Hero);
            writer.Write(friend.Level);
            writer.WritePersonName(friend.Name);
            writer.Write(friend.ClearTime);
        }
        
        writer.Write(Data.Chapter);
    }
}