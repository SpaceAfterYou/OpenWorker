using System.Collections.ObjectModel;
using OpenWorker.MazeServer.Services;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A maze server looks everything up before it uses up the reservation of a player: a location the table of mazes
/// does not have must turn the player away, not throw on the world loop with the reservation already spent.
/// </summary>
[TestClass]
public sealed class MazeEntryTest
{
    /// <summary>
    /// A row of the maze table that carries only its identifier; the rest of the columns are empty.
    /// </summary>
    private static MazeInfoRow Row(short id)
    {
        var bytes = new byte[1024];

        BitConverter.TryWriteBytes(bytes, id);

        return new MazeInfoRow(new BinaryReader(new MemoryStream(bytes)));
    }

    [TestMethod]
    public void RowOfTheLocationIsFound()
    {
        var table = new ReadOnlyCollection<MazeInfoRow>([Row(20001), Row(20002), Row(20003)]);

        Assert.IsTrue(PersonService.TryGetMazeInfo(table, 20002, out var info));
        Assert.AreEqual((short)20002, info.Id);
    }

    [TestMethod]
    public void LocationWithNoRowIsRefusedInsteadOfThrowing()
    {
        var table = new ReadOnlyCollection<MazeInfoRow>([Row(20001), Row(20003)]);

        Assert.IsFalse(PersonService.TryGetMazeInfo(table, 20002, out _));
    }

    [TestMethod]
    public void EmptyTableHasNoRow()
    {
        Assert.IsFalse(PersonService.TryGetMazeInfo(new ReadOnlyCollection<MazeInfoRow>([]), 20001, out _));
    }
}
