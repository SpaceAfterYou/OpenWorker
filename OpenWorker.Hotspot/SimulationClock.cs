using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OpenWorker.Hotspot;

/// <summary>
/// Something that lives in game time: monsters of a maze, and later anything else that moves on its own.
/// </summary>
public interface ISimulation
{
    /// <summary>
    /// Runs on the world loop. <paramref name="elapsed"/> is the time since the previous step,
    /// never more than <see cref="SimulationClock.MaxStep"/>.
    /// </summary>
    void Step(TimeSpan elapsed);
}

/// <summary>
/// One clock for every simulation of the server: all of them step together on the world loop, at a fixed
/// rate, and a step that takes longer than the period is reported.<br/>
/// When the loop falls behind, steps are skipped rather than repeated to catch up, and the time
/// of one step is capped, so a stall slows the game down instead of making monsters jump.
/// </summary>
public sealed class SimulationClock(WorldLoop loop, ILogger<SimulationClock> logger) : BackgroundService
{
    public static TimeSpan Period { get; } = TimeSpan.FromMilliseconds(50);
    public static TimeSpan MaxStep { get; } = Period * 4;

    /// <summary>
    /// How often an overloaded clock writes to the log.
    /// </summary>
    private static TimeSpan ReportInterval { get; } = TimeSpan.FromSeconds(10);

    // Touched on the world loop only.
    private readonly List<ISimulation> _simulations = [];

    private long _last = Stopwatch.GetTimestamp();
    private long _lastReport;
    private int _overBudget;
    private TimeSpan _worst;

    public int Count => _simulations.Count;

    /// <summary>
    /// Starts stepping <paramref name="simulation"/> from the next tick. Call on the world loop.
    /// </summary>
    public void Add(ISimulation simulation)
    {
        Debug.Assert(loop.IsCurrent || !IsRunning, "Simulations are added on the world loop.");

        if (!_simulations.Contains(simulation))
        {
            _simulations.Add(simulation);
        }
    }

    /// <summary>
    /// Stops stepping <paramref name="simulation"/>, also when called from inside its own step. Call on the world loop.
    /// </summary>
    public void Remove(ISimulation simulation)
    {
        Debug.Assert(loop.IsCurrent || !IsRunning, "Simulations are removed on the world loop.");

        _simulations.Remove(simulation);
    }

    private bool IsRunning { get; set; }

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() => RunAsync(cancellationToken), cancellationToken);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        IsRunning = true;

        using var timer = new PeriodicTimer(Period);

        try
        {
            // The next tick waits for this one, so a busy loop is never handed a queue of ticks.
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await loop.InvokeAsync(Tick);
            }
        }

        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    /// <summary>
    /// One step of every simulation. Public for tests, which drive the clock by hand.
    /// </summary>
    public void Tick()
    {
        var started = Stopwatch.GetTimestamp();
        var elapsed = Stopwatch.GetElapsedTime(_last, started);

        _last = started;

        if (elapsed > MaxStep)
        {
            elapsed = MaxStep;
        }

        // A step may add or remove simulations, its own included.
        foreach (var simulation in _simulations.ToArray())
        {
            try
            {
                simulation.Step(elapsed);
            }

            catch (Exception e)
            {
                logger.LogError(e, "Simulation {Simulation} failed to step.", simulation.GetType().Name);
            }
        }

        Measure(Stopwatch.GetElapsedTime(started));
    }

    private void Measure(TimeSpan duration)
    {
        if (duration <= Period)
        {
            return;
        }

        _overBudget++;

        if (duration > _worst)
        {
            _worst = duration;
        }

        if (Stopwatch.GetElapsedTime(_lastReport) < ReportInterval)
        {
            return;
        }

        logger.LogWarning(
            "{Count} steps took longer than {Budget} ms in the last {Interval} s, the worst {Worst} ms, with {Simulations} simulations.",
            _overBudget, Period.TotalMilliseconds, ReportInterval.TotalSeconds, _worst.TotalMilliseconds, _simulations.Count);

        _overBudget = 0;
        _worst = TimeSpan.Zero;
        _lastReport = Stopwatch.GetTimestamp();
    }
}
