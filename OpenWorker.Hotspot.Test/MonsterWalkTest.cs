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

    private static int Walks(HeldMonsterTest.Arena arena) => arena.Connection.Frames.Select(PlayerFrame.Open)
        .Count(e => e.Is(GroupOpcode.Move, (byte)MoveOpcode.MoveBt));
}
