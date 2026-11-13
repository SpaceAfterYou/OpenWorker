using Arch.Core;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Gameplay.Combat;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A maze measures time on the simulation clock that steps it, not on the clock of the process: a clock that stands, or
/// that caps its step after a stall, is the time of the maze.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MazeClockTest
{
    private static void Tick(FakeTimeProvider time, SimulationClock clock, int ticks = 1)
    {
        for (var tick = 0; tick < ticks; tick++)
        {
            time.Advance(SimulationClock.Period);
            clock.Tick();
        }
    }

    [TestMethod]
    public void ThePlayedTimeIsTheTimeTheMazeSimulated()
    {
        var world = World.Create();

        try
        {
            var time = new FakeTimeProvider();
            var clock = TestMazes.Clock(time);
            var maze = TestMazes.Create(world, world.Create(), clock: clock);

            Tick(time, clock, 40);

            Assert.AreEqual(0, maze.PlayedSeconds, "Not started yet.");
            Assert.IsTrue(maze.TryStart());

            Tick(time, clock, 60);

            Assert.AreEqual(3, maze.PlayedSeconds);

            // A stall of ten seconds is one capped step of the game.
            time.Advance(TimeSpan.FromSeconds(10));
            clock.Tick();

            Assert.AreEqual(3, maze.PlayedSeconds);

            maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }

    [TestMethod]
    public void TheGapBetweenCountedHitsIsTheMazesTime()
    {
        var world = World.Create();

        try
        {
            var time = new FakeTimeProvider();
            var clock = TestMazes.Clock(time);
            var maze = TestMazes.Create(world, MonsterArmorTest.Player(world), map: PartyMazeTest.MapWithMonster(99001), clock: clock);

            maze.SpawnSector(1);

            var monster = maze.Creatures.FindMonsters((_, _) => true).Single();

            world.Set(monster, world.Get<MonsterCombatComponent>(monster) with { CountsHits = true });

            Assert.AreEqual(1, maze.LifeTaken(monster, 500), "The first hit takes one point.");

            Thread.Sleep(600);

            Assert.AreEqual(0, maze.LifeTaken(monster, 500), "The clock of the maze has not moved.");

            Tick(time, clock, 9);

            Assert.AreEqual(0, maze.LifeTaken(monster, 500), "Too soon in game time.");

            Tick(time, clock);

            Assert.AreEqual(1, maze.LifeTaken(monster, 500));
        }

        finally
        {
            World.Destroy(world);
        }
    }
}
