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
    public void ATowerIsFifteenChaptersThenFiveStagesOfMyTimeAndTheBestClearThenTheChapter()
    {
        var bytes = Written(new RankingInfiniteTowerListResponse
        {
            Data = new RankingInfiniteTowerClearInfo
            {
                // Fewer chapters and stages than the client reads are filled with nothing.
                FriendsClearedPerChapter = [3, 1],
                Stages =
                [
                    new RankingInfiniteTowerRankingInfo
                    {
                        MyClearTime = 70, Chapter = 4, Stage = 1, Person = 9, Hero = Hero.Lily, Level = 30, Name = "Ab", ClearTime = 61
                    }
                ],
                Chapter = 4
            }
        });

        using var stream = new MemoryStream();
        using var expected = new BinaryWriter(stream);

        expected.Write(3);
        expected.Write(1);

        for (var chapter = 2; chapter < 15; chapter++)
        {
            expected.Write(0);
        }

        void Stage(int mine, int chapter, int stage, int person, byte hero, byte level, string name, int time)
        {
            expected.Write(mine);
            expected.Write(chapter);
            expected.Write(stage);
            expected.Write(person);
            expected.Write(hero);
            expected.Write(level);
            WriteClientString(expected, name);
            expected.Write(time);
        }

        Stage(70, 4, 1, 9, (byte)Hero.Lily, 30, "Ab", 61);

        for (var stage = 1; stage < 5; stage++)
        {
            Stage(0, 0, 0, 0, 0, 0, string.Empty, 0);
        }

        expected.Write(4);
        expected.Flush();

        CollectionAssert.AreEqual(stream.ToArray(), bytes);
    }
}
