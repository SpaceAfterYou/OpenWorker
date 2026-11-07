using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Maze.Responses;
using OpenWorker.Hotspot.Modules.Maze.Types;
using OpenWorker.Hotspot.Modules.Monster.Responses;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Hotspot.Modules.World.Responses;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class MazeWireFormatTest
{
    private static byte[] Written(IResponseHotspotMessage message)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        message.Write(writer);
        writer.Flush();

        return stream.ToArray();
    }

    [TestMethod]
    public void MonsterSuperArmorIsActorAndTwoFloats()
    {
        var data = Written(new MonsterSuperArmorGageResponse { Current = 1.5f, Max = 2.5f });

        Assert.HasCount(12, data);
        Assert.AreEqual(1.5f, BitConverter.ToSingle(data, 4));
        Assert.AreEqual(2.5f, BitConverter.ToSingle(data, 8));
    }

    [TestMethod]
    public void MonsterPartsAreActorAndFourNumbers()
    {
        var data = Written(new MonsterPartsHpResponse { FirstCurrent = 1, FirstMax = 2, SecondCurrent = 3, SecondMax = 4 });

        Assert.HasCount(20, data);
        Assert.AreEqual(1, BitConverter.ToInt32(data, 4));
        Assert.AreEqual(4, BitConverter.ToInt32(data, 16));
    }

    [TestMethod]
    public void MonsterStatListPutsTheValueBeforeTheStat()
    {
        var data = Written(new MonsterUpdateStatListResponse
        {
            Stats = [new StatKeyValuePair { byIndex = STAT_TYPE.STAT_HP_MAX, statValue = 7f }]
        });

        Assert.HasCount(4 + 1 + 4 + 2, data);
        Assert.AreEqual(1, data[4]);
        Assert.AreEqual(7f, BitConverter.ToSingle(data, 5));
        Assert.AreEqual((short)STAT_TYPE.STAT_HP_MAX, BitConverter.ToInt16(data, 9));
    }

    [TestMethod]
    public void MonsterDieIsActorMotionAndType()
    {
        var bytes = Written(new MonsterDieResponse { Actor = 7, Motion = 8, DieType = 9 });

        Assert.HasCount(4 + 2 + 1, bytes);
        Assert.AreEqual((short)8, BitConverter.ToInt16(bytes, 4));
        Assert.AreEqual((byte)9, bytes[6]);
    }

    [TestMethod]
    public void PortalEnableIsBoxAndTwoFlags()
    {
        var bytes = Written(new WorldWarpEnableResponse { Box = 30008, Open = true });

        Assert.HasCount(4 + 1 + 1, bytes);
        Assert.AreEqual(30008, BitConverter.ToInt32(bytes));
        Assert.AreEqual((byte)1, bytes[4]);
        Assert.AreEqual((byte)0, bytes[5]);
    }

    [TestMethod]
    public void GateEnableIsBoxAndFlag()
    {
        Assert.HasCount(4 + 1, Written(new WorldGateEnableResponse { Gate = 20001, Open = true }));
    }

    [TestMethod]
    public void SectorInfosEndWithTheCurrentSector()
    {
        var bytes = Written(new MazeSectorInfosResponse { Cleared = [1, 2], Current = 3 });

        Assert.HasCount(4 + 2 * 4 + 4, bytes);
        Assert.AreEqual(2, BitConverter.ToInt32(bytes));
        Assert.AreEqual(3, BitConverter.ToInt32(bytes, 12));
    }

    [TestMethod]
    public void GateInfosStartWithTheReEnterFlag()
    {
        var bytes = Written(new MazeGateInfosResponse { ReEnter = true, Open = [7] });

        Assert.HasCount(1 + 4 + 4, bytes);
        Assert.AreEqual((byte)1, bytes[0]);
        Assert.AreEqual(1, BitConverter.ToInt32(bytes, 1));
    }

    [TestMethod]
    public void PortalInfosHaveBothCountsFirst()
    {
        var bytes = Written(new MazePortalInfosResponse { Open = [1, 2], Closed = [3] });

        Assert.HasCount(4 + 4 + 3 * 4, bytes);
        Assert.AreEqual(2, BitConverter.ToInt32(bytes));
        Assert.AreEqual(1, BitConverter.ToInt32(bytes, 4));
        Assert.AreEqual(3, BitConverter.ToInt32(bytes, 16));
    }

    [TestMethod]
    public void InteractionInfosHaveAByteCount()
    {
        var bytes = Written(new MazeInteractionInfosResponse
        {
            Interactions = [new MazeInteractionValue(true, false, 701, 1)]
        });

        Assert.HasCount(1 + 1 + 1 + 4 + 4, bytes);
        Assert.AreEqual(701, BitConverter.ToInt32(bytes, 3));
    }

    [TestMethod]
    public void InteractionRunStartsWithTheResult()
    {
        var bytes = Written(new MazeInteractionRunResponse
        {
            Result = 0,
            Interaction = new MazeInteractionValue(true, true, 701, 1)
        });

        Assert.HasCount(1 + 1 + 1 + 4 + 4, bytes);
        Assert.AreEqual(701, BitConverter.ToInt32(bytes, 3));
    }
}
