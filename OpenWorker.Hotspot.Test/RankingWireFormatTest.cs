using System.Text;
using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Ranking.Responses;
using OpenWorker.Hotspot.Modules.Ranking.Types;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// What the client reads of the three rankings.
/// </summary>
[TestClass]
public sealed class RankingWireFormatTest
{
    private static byte[] Written(IResponseHotspotMessage message)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        message.Write(writer);
        writer.Flush();

        return stream.ToArray();
    }

    private static void WriteClientString(BinaryWriter writer, string value)
    {
        writer.Write((short)(2 * value.Length + 1));
        writer.Write(Encoding.Unicode.GetBytes(value));
        writer.Write((byte)0);
    }

    private static ST_RANKING_TIME_ATTACK_USER Clear(int person, string name, int time) => new()
    {
        Person = person,
        World = 7,
        Order = 2,
        Hero = Hero.Lily,
        Level = 25,
        Name = name,
        Time = time,
        Score = 90
    };

    [TestMethod]
    public void ATimeAttackListIsThePlayerThenTheMapAndTheCountedRows()
    {
        var bytes = Written(new RankingTimeAttackListResponse
        {
            Data = new ST_RANKING_TIME_ATTACK_USER2
            {
                Player = Clear(1, "Me", 100),
                ParticipantList = new ST_RANKING_TIME_ATTACK_USER3
                {
                    World = 7,
                    aUserList = [Clear(2, "Ab", 80), Clear(3, "Cd", 90)]
                }
            }
        });

        using var stream = new MemoryStream();
        using var expected = new BinaryWriter(stream);

        void Row(int person, string name, int time)
        {
            expected.Write(person);
            expected.Write(7);
            expected.Write((short)2);
            expected.Write((byte)Hero.Lily);
            expected.Write((byte)25);
            WriteClientString(expected, name);
            expected.Write(time);
            expected.Write(90);
        }

        Row(1, "Me", 100);
        expected.Write(7);
        expected.Write((short)2);
        Row(2, "Ab", 80);
        Row(3, "Cd", 90);
        expected.Flush();

        CollectionAssert.AreEqual(stream.ToArray(), bytes);
    }

    [TestMethod]
    public void AnArenaListIsTheTypeThePlayerAndTheCountedRows()
    {
        ST_USER_RANKING_INFO Row(int person, string name, int score) => new()
        {
            Person = person,
            Order = 3,
            Hero = Hero.Jin,
            Level = 25,
            Name = name,
            KillScore = score
        };

        var bytes = Written(new RankingPvpListResponse
        {
            Data = new PS_RANKING_LIST_RES
            {
                Type = RankingType.Seasonal,
                Player = Row(1, "Me", 5),
                ParticipantList = [Row(2, "Ab", 9)]
            }
        });

        using var stream = new MemoryStream();
        using var expected = new BinaryWriter(stream);

        void Write(int person, string name, int score)
        {
            expected.Write(person);
            expected.Write((short)3);
            expected.Write((byte)Hero.Jin);
            expected.Write((byte)25);
            WriteClientString(expected, name);
            expected.Write(score);
        }

        expected.Write((byte)RankingType.Seasonal);
        Write(1, "Me", 5);
        expected.Write((short)1);
        Write(2, "Ab", 9);
        expected.Flush();

        CollectionAssert.AreEqual(stream.ToArray(), bytes);
    }

    [TestMethod]
    public void AnEmptyTowerIsFifteenFloorsFiveRowsOfFourteenBytesAndAChapter()
    {
        var bytes = Written(RankingInfiniteTowerListResponse.Create(4));

        // A row is the clear time and the stage, the person, the hero and level, the name and the time of the best.
        var row = 4 + 4 + 4 + 1 + 1 + 2 + 1 + 4;

        Assert.HasCount(15 * 4 + 5 * row + 4, bytes);
        Assert.AreEqual(4, BitConverter.ToInt32(bytes, bytes.Length - 4));
        Assert.IsTrue(bytes[..(15 * 4)].All(e => e == 0));
    }
}
