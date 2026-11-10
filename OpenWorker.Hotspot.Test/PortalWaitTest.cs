using Arch.Core;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Lua;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A portal that leads to another place on the map takes everyone in the maze along, so it waits for them: at once when all
/// stand at it, after a countdown when half of them do, and not at all when too few do. It does not run beside other tests: the
/// components of a world are numbered by the first test that uses them, and two tests that do it at once can get one number.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class PortalWaitTest
{
    private static readonly PortalWait.Door Door = new(Portal: 10, Jump: 20);
    private static readonly int[] Countdown = [5, 4, 3, 2, 1, 0];

    private sealed class Maze : IDisposable
    {
        public World World { get; } = World.Create();
        public List<int> Told { get; } = [];
        public List<PortalWait.Door> Opened { get; } = [];
        public int Users { get; set; }
        public PortalWait Wait { get; }

        public Maze(int users)
        {
            Users = users;
            Wait = new PortalWait(() => Users, Told.Add, Opened.Add);
        }

        public Entity Player() => World.Create(new LevelComponent(1, 0));

        public void Dispose() => World.Dispose();
    }

    [TestMethod]
    public void APortalOpensAtOnceWhenEveryoneStandsAtIt()
    {
        using var maze = new Maze(users: 3);
        var players = Enumerable.Range(0, 3).Select(_ => maze.Player()).ToArray();

        maze.Wait.Add(players[0], Door);
        maze.Wait.Add(players[1], Door);

        Assert.IsEmpty(maze.Opened, "Two of three do not open it");

        maze.Wait.Add(players[2], Door);

        CollectionAssert.AreEqual(new[] { Door }, maze.Opened);
        Assert.AreEqual(0, maze.Wait.WaitingAt(Door.Portal), "Nobody is left standing at it");
    }

    [TestMethod]
    public void AloneAPlayerOpensThePortalAtOnce()
    {
        using var maze = new Maze(users: 1);

        maze.Wait.Add(maze.Player(), Door);

        CollectionAssert.AreEqual(new[] { Door }, maze.Opened);
        Assert.IsEmpty(maze.Told, "A portal that opens at once counts nothing down");
    }

    [TestMethod]
    public void HalfOfThePlayersStartACountdownAfterWhichThePortalOpensForAll()
    {
        using var maze = new Maze(users: 4);

        maze.Wait.Add(maze.Player(), Door);

        Assert.IsFalse(maze.Wait.IsCounting, "One of four is less than half");

        maze.Wait.Add(maze.Player(), Door);

        Assert.IsTrue(maze.Wait.IsCounting, "Two of four is half");

        for (var step = 0; step < 200 && maze.Opened.Count == 0; step++)
        {
            maze.Wait.Advance(0.05f);
        }

        CollectionAssert.AreEqual(Countdown, maze.Told, "The seconds are told, and the end");
        CollectionAssert.AreEqual(new[] { Door }, maze.Opened);
        Assert.IsFalse(maze.Wait.IsCounting);
    }

    [TestMethod]
    public void SteppingOutOfThePortalOrLeavingTheMazeStopsTheCountdown()
    {
        using var maze = new Maze(users: 4);
        var first = maze.Player();
        var second = maze.Player();

        maze.Wait.Add(first, Door);
        maze.Wait.Add(second, Door);
        maze.Wait.Advance(2f);

        maze.Wait.Remove(second);

        Assert.IsFalse(maze.Wait.IsCounting, "One of four is not half");
        Assert.AreEqual(0, maze.Told[^1], "The clients are told the countdown is off");

        maze.Wait.Advance(10f);

        Assert.IsEmpty(maze.Opened, "A portal that nobody waits at never opens");

        // Two stand at it again; then two of the four leave the maze, and the two at the portal are all that is left.
        maze.Wait.Add(second, Door);

        Assert.IsTrue(maze.Wait.IsCounting);

        maze.Users = 2;
        maze.Wait.Remove(maze.Player());

        CollectionAssert.AreEqual(new[] { Door }, maze.Opened, "When the rest are all at the portal it opens");
    }
}
