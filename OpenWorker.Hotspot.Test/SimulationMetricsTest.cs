using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Hotspot.Telemetry;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class SimulationMetricsTest
{
    private sealed class Sleeper(TimeSpan time) : ISimulation
    {
        public void Advance(TimeSpan elapsed)
        {
            Thread.Sleep(time);
        }
    }

    private static SimulationClock Create(MetricsHost metrics) =>
        new(new WorldLoop(NullLogger<WorldLoop>.Instance), NullLogger<SimulationClock>.Instance, metrics.Get<SimulationMetrics>());

    [TestMethod]
    public void EveryTickRecordsItsDurationAndOnlyALongOneIsOverBudget()
    {
        using var metrics = new MetricsHost();
        using var steps = metrics.Listen<double>("simulation.step.duration");
        using var overBudget = metrics.Listen<long>("simulation.overbudget");
        var clock = Create(metrics);

        var fast = new Sleeper(TimeSpan.Zero);

        clock.Add(fast);
        clock.Tick();

        Assert.HasCount(1, steps.GetMeasurementSnapshot());
        Assert.IsLessThan(SimulationClock.Period.TotalSeconds, steps.LastMeasurement!.Value);
        Assert.AreEqual(0, overBudget.Sum(), "A tick inside the budget is not over it.");

        clock.Remove(fast);
        clock.Add(new Sleeper(SimulationClock.Period + TimeSpan.FromMilliseconds(20)));
        clock.Tick();

        Assert.HasCount(2, steps.GetMeasurementSnapshot());
        Assert.IsGreaterThanOrEqualTo(SimulationClock.Period.TotalSeconds, steps.LastMeasurement.Value, "The duration is in seconds.");
        Assert.AreEqual(1, overBudget.Sum());
    }

    [TestMethod]
    public void CountIsTheSimulationsTheClockSteps()
    {
        using var metrics = new MetricsHost();
        using var count = metrics.Listen<int>("simulation.count");
        var clock = Create(metrics);

        var first = new Sleeper(TimeSpan.Zero);
        var second = new Sleeper(TimeSpan.Zero);

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
