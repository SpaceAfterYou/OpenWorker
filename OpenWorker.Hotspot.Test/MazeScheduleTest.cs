using Arch.Core;
using Lua;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Lua;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// What a maze waits for lives on the simulation clock, and a maze that closes takes all of it with it: no script of a
/// player who has left hears a timer or a delayed event.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MazeScheduleTest
{
    private const string Script = """
        heard = 0
        function OnTimerComplete(timer, maze) heard = heard + 1 end
        function OnLater(first, second, third, maze) heard = heard + 1 end
        function OnNpcRotation(npc, key, maze) heard = heard + 1 end
        """;

    private static async Task<(LuaMaze Maze, LuaState State)> StartAsync(World world, SimulationClock clock)
    {
        var state = LuaState.Create();

        await state.DoStringAsync(Script);

        var maze = TestMazes.Create(world, world.Create(), clock: clock, state: state);

        Assert.IsTrue(maze.TryStart());

        // A plain timer, a timer with a function, and an event the script hears a second later.
        maze.AddTimer(7, 0.1f);
        maze.AddTimerEx("OnLater", 0.2f, 1, 2, 3);
        maze.SetNpcRotation(3, 90f, 9);

        return (maze, state);
    }

    private static double Heard(LuaState state) => state.Environment["heard"].Read<double>();

    [TestMethod]
    public async Task ClosingAMazeCancelsEverythingItHasScheduled()
    {
        var world = World.Create();

        try
        {
            var time = new FakeTimeProvider();
            var clock = TestMazes.Clock(time);
            var (open, heard) = await StartAsync(world, clock);
            var (closed, deaf) = await StartAsync(world, clock);

            Assert.IsTrue(closed.Scheduled > 0);

            closed.Close();

            Assert.AreEqual(0, closed.Scheduled);
            Assert.AreEqual(1, clock.Count);

            // Two seconds of game time: every timer and the delayed event are due long before.
            for (var tick = 0; tick < 40; tick++)
            {
                time.Advance(SimulationClock.Period);
                clock.Tick();
            }

            Assert.AreEqual(3d, Heard(heard));
            Assert.AreEqual(0d, Heard(deaf));
            Assert.AreEqual(0, clock.Pending);

            // A closed maze takes nothing more on the clock.
            closed.SetNpcRotation(3, 0f, 1);

            Assert.AreEqual(0, closed.Scheduled);
            Assert.AreEqual(0, clock.Pending);

            open.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }
}
