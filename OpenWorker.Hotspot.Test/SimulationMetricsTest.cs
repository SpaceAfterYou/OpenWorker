using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Hotspot.Telemetry;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class SimulationMetricsTest
{
    /// <summary>
    /// A simulation whose step takes the given time on the clock of the test.
    /// </summary>
    private sealed class Worker(FakeTimeProvider time, TimeSpan length) : ISimulation
    {
        public void Advance(TimeSpan elapsed)
        {
            time.Advance(length);
        }
    }

    private static SimulationClock Create(MetricsHost metrics, FakeTimeProvider time) =>
        new(new WorldLoop(NullLogger<WorldLoop>.Instance), NullLogger<SimulationClock>.Instance, metrics.Get<SimulationMetrics>(), time);

    /// <summary>
    /// The length of a step is measured on the clock the game time is read from: here the clock of the test, which only the
    /// steps move, so the lengths recorded are exact whatever else the machine is doing.
    /// </summary>
    [TestMethod]
    public void EveryTickRecordsItsDurationAndOnlyALongOneIsOverBudget()
    {
        using var metrics = new MetricsHost();
        using var steps = metrics.Listen<double>("simulation.step.duration");
        using var overBudget = metrics.Listen<long>("simulation.overbudget");
        var time = new FakeTimeProvider();
        var clock = Create(metrics, time);

        var fast = new Worker(time, TimeSpan.FromMilliseconds(10));
        var slow = SimulationClock.Period + TimeSpan.FromMilliseconds(20);

        clock.Add(fast);
        clock.Tick();

        Assert.HasCount(1, steps.GetMeasurementSnapshot());
        Assert.AreEqual(0.010, steps.LastMeasurement!.Value, 1e-9, "The duration is in seconds.");
        Assert.AreEqual(0, overBudget.Sum(), "A tick inside the budget is not over it.");

        clock.Remove(fast);
        clock.Add(new Worker(time, slow));
        clock.Tick();

        Assert.HasCount(2, steps.GetMeasurementSnapshot());
        Assert.AreEqual(slow.TotalSeconds, steps.LastMeasurement.Value, 1e-9);
        Assert.AreEqual(1, overBudget.Sum());
    }

    [TestMethod]
    public void CountIsTheSimulationsTheClockSteps()
    {
        using var metrics = new MetricsHost();
        using var count = metrics.Listen<int>("simulation.count");
        var time = new FakeTimeProvider();
        var clock = Create(metrics, time);

        var first = new Worker(time, TimeSpan.Zero);
        var second = new Worker(time, TimeSpan.Zero);

        count.RecordObservableInstruments();
        Assert.AreEqual(0, count.LastMeasurement!.Value);

        clock.Add(first);
        clock.Add(second);
        clock.Add(first);

        count.RecordObservableInstruments();
        Assert.AreEqual(2, count.LastMeasurement.Value, "A simulation added twice is stepped once.");

        clock.Remove(first);

        count.RecordObservableInstruments();
        Assert.AreEqual(1, count.LastMeasurement.Value);
    }
}
