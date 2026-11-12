using Microsoft.Extensions.Time.Testing;
using Microsoft.Extensions.Logging.Abstractions;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class SimulationClockTest
{
    private sealed class Recorder(Action<Recorder>? onStep = null) : ISimulation
    {
        public List<TimeSpan> Steps { get; } = [];

        public void Advance(TimeSpan elapsed)
        {
            Steps.Add(elapsed);

            onStep?.Invoke(this);
        }
    }

    private static SimulationClock Create() =>
        new(new WorldLoop(NullLogger<WorldLoop>.Instance), NullLogger<SimulationClock>.Instance);

    [TestMethod]
    public void EverySimulationStepsOncePerTick()
    {
        var clock = Create();
        var first = new Recorder();
        var second = new Recorder();

        clock.Add(first);
        clock.Add(second);
        clock.Add(first);

        clock.Tick();
        clock.Tick();

        Assert.HasCount(2, first.Steps);
        Assert.HasCount(2, second.Steps);
    }

    [TestMethod]
    public void LongPauseIsCappedToOneStep()
    {
        var time = new FakeTimeProvider();
        var clock = new SimulationClock(new WorldLoop(NullLogger<WorldLoop>.Instance), NullLogger<SimulationClock>.Instance, time: time);
        var recorder = new Recorder();

        clock.Add(recorder);

        // The clock was made long before this tick: far more than MaxStep has passed.
        time.Advance(SimulationClock.MaxStep * 2);

        clock.Tick();

        Assert.AreEqual(SimulationClock.MaxStep, recorder.Steps.Single());
    }

    [TestMethod]
    public void SimulationCanLeaveDuringItsOwnStep()
    {
        var clock = Create();
        var leaving = new Recorder(self => clock.Remove(self));
        var staying = new Recorder();

        clock.Add(leaving);
        clock.Add(staying);

        clock.Tick();
        clock.Tick();

        Assert.HasCount(1, leaving.Steps);
        Assert.HasCount(2, staying.Steps);
        Assert.AreEqual(1, clock.Count);
    }

    [TestMethod]
    public void SimulationRemovedByAnotherDuringATickIsNotSteppedAgain()
    {
        var clock = Create();
        var later = new Recorder();
        var first = new Recorder(_ => clock.Remove(later));

        clock.Add(first);
        clock.Add(later);

        clock.Tick();

        Assert.IsEmpty(later.Steps);
        Assert.AreEqual(1, clock.Count);
    }

    [TestMethod]
    public void ScheduledActionRunsAtItsDueTickAndNotBefore()
    {
        var time = new FakeTimeProvider();
        var clock = new SimulationClock(new WorldLoop(NullLogger<WorldLoop>.Instance), NullLogger<SimulationClock>.Instance, time: time);
        var ran = new List<TimeSpan>();
        var cancelled = clock.Schedule(SimulationClock.Period, () => ran.Add(TimeSpan.MinValue));

        clock.Schedule(SimulationClock.Period * 3, () => ran.Add(clock.Now));
        clock.Schedule(TimeSpan.Zero, () => ran.Add(clock.Now));
        Assert.IsTrue(cancelled.Cancel());

        for (var tick = 0; tick < 5; tick++)
        {
            time.Advance(SimulationClock.Period);
            clock.Tick();
        }

        // A delay of nothing is the next tick, three periods are the third; the cancelled one never runs.
        CollectionAssert.AreEqual(new[] { SimulationClock.Period, SimulationClock.Period * 3 }, ran);
        Assert.IsFalse(cancelled.IsPending);
        Assert.AreEqual(0, clock.Pending);
    }

    [TestMethod]
    public void FailingSimulationDoesNotStopTheOthers()
    {
        var clock = Create();
        var failing = new Recorder(_ => throw new InvalidOperationException());
        var healthy = new Recorder();

        clock.Add(failing);
        clock.Add(healthy);

        clock.Tick();

        Assert.HasCount(1, healthy.Steps);
        Assert.AreEqual(2, clock.Count);
    }
}
