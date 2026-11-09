using System.Diagnostics.Metrics;

namespace OpenWorker.Hotspot.Telemetry;

/// <summary>
/// What <c>SimulationClock</c> reports: how long a step of all the simulations takes against its 50 ms budget,
/// and how many simulations (mazes) the process runs.
/// </summary>
public sealed class SimulationMetrics
{
    private readonly Histogram<double> _stepDuration;
    private readonly Counter<long> _overBudget;

    // Written by the clock on the world loop, read by whoever collects the gauge. The process has one clock.
    private int _count;

    public SimulationMetrics(IMeterFactory factory)
    {
        var meter = OpenWorkerMeter.Create(factory);

        _stepDuration = meter.CreateDuration(
            "simulation.step.duration",
            "How long one tick of the simulation clock takes, all the simulations together.");

        _overBudget = meter.CreateCounter<long>(
            "simulation.overbudget",
            unit: "{step}",
            description: "Ticks that took longer than the period of the clock.");

        meter.CreateObservableGauge(
            "simulation.count",
            () => Volatile.Read(ref _count),
            unit: "{simulation}",
            description: "Simulations (mazes) the clock steps.");
    }

    internal void SetCount(int count)
    {
        Volatile.Write(ref _count, count);
    }

    internal void Step(TimeSpan duration)
    {
        _stepDuration.Record(duration.TotalSeconds);
    }

    internal void OverBudget()
    {
        _overBudget.Add(1);
    }
}
