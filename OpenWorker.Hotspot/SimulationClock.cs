using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenWorker.Hotspot.Telemetry;

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
    void Advance(TimeSpan elapsed);
}

/// <summary>
/// One clock for every simulation of the server: all of them step together on the world loop, at a fixed
/// rate, and a step that takes longer than the period is reported.<br/>
/// When the loop falls behind, steps are skipped rather than repeated to catch up, and the time
/// of one step is capped, so a stall slows the game down instead of making monsters jump.
/// </summary>
/// <param name="metrics">Reports the steps and the simulations; <see langword="null"/> for a clock nobody measures.</param>
public sealed partial class SimulationClock(WorldLoop loop, ILogger<SimulationClock> logger, SimulationMetrics? metrics = null) : BackgroundService
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

            metrics?.SetCount(_simulations.Count);
        }
    }

    /// <summary>
    /// Stops stepping <paramref name="simulation"/>, also when called from inside its own step. Call on the world loop.
    /// </summary>
    public void Remove(ISimulation simulation)
    {
        Debug.Assert(loop.IsCurrent || !IsRunning, "Simulations are removed on the world loop.");

        _simulations.Remove(simulation);

        metrics?.SetCount(_simulations.Count);
    }

    private bool IsRunning { get; set; }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => RunAsync(stoppingToken), stoppingToken);
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
                simulation.Advance(elapsed);
            }

            catch (Exception e)
            {
                LogStepFailed(e, simulation.GetType().Name);
            }
        }

        Measure(Stopwatch.GetElapsedTime(started));
    }

    private void Measure(TimeSpan duration)
    {
        metrics?.Step(duration);

        if (duration <= Period)
        {
            return;
        }

        metrics?.OverBudget();

        _overBudget++;

        if (duration > _worst)
        {
            _worst = duration;
        }

        if (Stopwatch.GetElapsedTime(_lastReport) < ReportInterval)
        {
            return;
        }

        LogOverBudget(_overBudget, Period.TotalMilliseconds, ReportInterval.TotalSeconds, _worst.TotalMilliseconds, _simulations.Count);

        _overBudget = 0;
        _worst = TimeSpan.Zero;
        _lastReport = Stopwatch.GetTimestamp();
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Simulation {Simulation} failed to step.")]
    private partial void LogStepFailed(Exception exception, string simulation);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "{Count} steps took longer than {Budget} ms in the last {Interval} s, the worst {Worst} ms, with {Simulations} simulations.")]
    private partial void LogOverBudget(int count, double budget, double interval, double worst, int simulations);
}
