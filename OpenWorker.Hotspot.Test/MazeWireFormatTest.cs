using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Maze.Responses;
using OpenWorker.Hotspot.Modules.Maze.Types;
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
