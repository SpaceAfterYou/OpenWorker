using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// What the clients are told of a monster's walk.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MonsterWalkTest
{
    /// <summary>
    /// A monster that has stopped and is told to stop again where it stands is not told to the clients twice: a second stop
    /// makes the client walk it back to the place of the message.
    /// </summary>
    [TestMethod]
    public Task AStopIsToldOncePerPlace() => HeldMonsterTest.WithArenaAsync(async arena =>
    {
        Assert.IsTrue(arena.Body.MoveTo(arena.Far, run: true));

        arena.Advance(20);

        await MetricAssertions.UntilAsync(() => Walks(arena) > 0, "The walk is told.");

        var before = HeldMonsterTest.Stops(arena).Length;

        arena.Body.StopMoving(true);
        arena.Body.StopMoving(true);

        // The next walk is told after the stops, so once it is there every stop is.
        var walks = Walks(arena);

        Assert.IsTrue(arena.Body.MoveTo(arena.Far, run: true));
        arena.Advance(1);

        await MetricAssertions.UntilAsync(() => Walks(arena) > walks, "The next walk is told.");

        Assert.HasCount(before + 1, HeldMonsterTest.Stops(arena));
    });

    /// <summary>
    /// A client that comes in while a monster walks is told the walk from where the monster is, after the monster itself.
    /// </summary>
    [TestMethod]
    public Task AClientThatComesInIsToldAWalkInProgress() => HeldMonsterTest.WithArenaAsync(async arena =>
    {
        Assert.IsTrue(arena.Body.MoveTo(arena.Far, run: true));

        arena.Advance(10);

        var late = new RecordingConnection();

        arena.Maze.Creatures.ShowAllTo(new ServerSessionComponent(late));

        await MetricAssertions.UntilAsync(() => Walks(late) > 0, "The walk is told to the client that came in.");

        var walk = late.Frames.Select(PlayerFrame.Open).First(e => e.Is(GroupOpcode.Move, (byte)MoveOpcode.MoveBt));

        Assert.AreEqual(arena.Position.X, walk.X, 1f);
    });

    private static int Walks(HeldMonsterTest.Arena arena) => Walks(arena.Connection);

    private static int Walks(RecordingConnection connection) => connection.Frames.Select(PlayerFrame.Open)
        .Count(e => e.Is(GroupOpcode.Move, (byte)MoveOpcode.MoveBt));
}
