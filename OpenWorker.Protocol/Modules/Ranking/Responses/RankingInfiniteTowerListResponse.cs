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

[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct RankingInfiniteTowerListResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Ranking;
    private const RankingOpcode Command = RankingOpcode.InfiniteTowerList;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public RankingInfiniteTowerClearInfo Data { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
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
            writer.Write(friend.Stage);
            writer.Write(friend.Person);
            writer.Write(friend.Hero);
            writer.Write(friend.Level);
            writer.WritePersonName(friend.Name);
            writer.Write(friend.ClearTime);
        }

        writer.Write(Data.Chapter);
    }

#endregion Interface: IWritableData

    /// <summary>
    /// Nobody has climbed the tower on this server yet, so it has nothing to show but the chapter that was asked for.
    /// </summary>
    public static RankingInfiniteTowerListResponse Create(int chapter)
    {
        return new RankingInfiniteTowerListResponse
        {
            Data = new RankingInfiniteTowerClearInfo
            {
                Chapter = chapter,
                FriendPassedPerFloor = new int[15],
                BestOfStage = Enumerable
                    .Range(0, 5)
                    .Select(_ => new RankingInfiniteTowerRankingInfo
                    {
                        MyClearTime = 0,
                        Stage = 0,
                        Person = 0,
                        Hero = 0,
                        Level = 0,
                        Name = string.Empty,
                        ClearTime = 0
                    })
                    .ToArray()
            }
        };
    }
}
