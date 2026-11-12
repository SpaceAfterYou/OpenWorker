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
/// of one step is capped, so a stall slows the game down instead of making monsters jump.<br/>
/// The clock also keeps the actions of the game that wait for a moment (<see cref="Schedule(TimeSpan, Action)"/>): a
/// queue ordered by the game time they are due at, run at the first tick that reaches it, before the simulations step.
/// </summary>
/// <param name="metrics">Reports the steps and the simulations; <see langword="null"/> for a clock nobody measures.</param>
/// <param name="time">Where the clock reads the time; a test gives a fake one and moves it by hand.</param>
public sealed partial class SimulationClock(WorldLoop loop, ILogger<SimulationClock> logger, SimulationMetrics? metrics = null, TimeProvider? time = null) : BackgroundService
{
    public static TimeSpan Period { get; } = TimeSpan.FromMilliseconds(50);
    public static TimeSpan MaxStep { get; } = Period * 4;

    /// <summary>
    /// How often an overloaded clock writes to the log.
    /// </summary>
    private static TimeSpan ReportInterval { get; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Cancelled actions are left in the queue until they come up, unless there are this many and they are most of it.
    /// </summary>
    private const int CompactAbove = 256;

    /// <summary>
    /// Spare entries of the queue kept for reuse at most.
    /// </summary>
    private const int MaxSpare = 4096;

    private readonly TimeProvider _time = time ?? TimeProvider.System;

    // Touched on the world loop only.
    private readonly StepList<ISimulation> _simulations = new();
    private readonly PriorityQueue<Scheduled, (long Due, long Order)> _queue = new();
    private readonly Stack<Scheduled> _spare = new();
    private readonly List<(Scheduled Item, (long Due, long Order) Priority)> _kept = [];

    private long _last = (time ?? TimeProvider.System).GetTimestamp();
    private long _lastReport;
    private int _overBudget;
    private TimeSpan _worst;
    private long _order;
    private int _cancelled;

    public int Count => _simulations.Count;

    /// <summary>
    /// The game time of the last tick: the steps added up, each a whole number of periods, at least one and at most
    /// <see cref="MaxStep"/>. It does not move between ticks.
    /// </summary>
    public TimeSpan Now { get; private set; }

    /// <summary>
    /// The game time at this moment: <see cref="Now"/> and the real time since that tick, at most one step. A handler
    /// between ticks reads a later time than the tick before it did; at a tick it is <see cref="Now"/>, give or take
    /// the jitter of the timer, which is why it may stand a moment behind a reading taken just before the tick.
    /// </summary>
    public TimeSpan Time
    {
        get
        {
            var since = _time.GetElapsedTime(_last);

            return Now + (since > MaxStep ? MaxStep : since);
        }
    }

    /// <summary>
    /// Actions that wait to run and have not been cancelled.
    /// </summary>
    public int Pending => _queue.Count - _cancelled;

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

    /// <summary>
    /// Runs <paramref name="action"/> on the world loop at the first tick whose game time is at least
    /// <paramref name="delay"/> past <see cref="Now"/>; never on the tick that is running, so a delay of zero is the
    /// next tick. Actions due at the same tick run in the order they were scheduled. Call on the world loop.
    /// </summary>
    public ScheduledAction Schedule(TimeSpan delay, Action action)
    {
        return Schedule(delay, action, null);
    }

    /// <summary>
    /// A group of actions that one owner can call off together (<see cref="ScheduleGroup.Close"/>).
    /// </summary>
    public ScheduleGroup CreateGroup()
    {
        return new ScheduleGroup(this);
    }

    internal ScheduledAction Schedule(TimeSpan delay, Action action, ScheduleGroup? group)
    {
        return ScheduleAt(Now + (delay > TimeSpan.Zero ? delay : TimeSpan.Zero), action, group);
    }

    /// <summary>
    /// Runs <paramref name="action"/> at the first tick whose <see cref="Now"/> reaches <paramref name="due"/>, and
    /// never on the tick that is running.
    /// </summary>
    internal ScheduledAction ScheduleAt(TimeSpan due, Action action, ScheduleGroup? group)
    {
        Debug.Assert(loop.IsCurrent || !IsRunning, "Actions are scheduled on the world loop.");
        ArgumentNullException.ThrowIfNull(action);

        var item = _spare.TryPop(out var spare) ? spare : new Scheduled();

        item.Action = action;
        item.Group = group;
        group?.Add(item);

        _queue.Enqueue(item, (Math.Max(due.Ticks, Now.Ticks + 1), _order++));

        return new ScheduledAction(this, item);
    }

    internal bool Cancel(Scheduled item)
    {
        item.Group?.Remove(item);

        Forget(item);

        return true;
    }

    /// <summary>
    /// The action will not run. It stays in the queue until it comes up or the queue is compacted.
    /// </summary>
    internal void Forget(Scheduled item)
    {
        item.Action = null;
        item.Group = null;

        _cancelled++;

        if (_cancelled > CompactAbove && _cancelled * 2 > _queue.Count)
        {
            Compact();
        }
    }

    private void Compact()
    {
        _kept.Clear();

        foreach (var (item, priority) in _queue.UnorderedItems)
        {
            if (item.Action is null)
            {
                Recycle(item);
            }
            else
            {
                _kept.Add((item, priority));
            }
        }

        _queue.Clear();

        foreach (var (item, priority) in _kept)
        {
            _queue.Enqueue(item, priority);
        }

        _kept.Clear();
        _cancelled = 0;
    }

    private void Recycle(Scheduled item)
    {
        // A handle that still names it no longer reaches the action that takes its place.
        item.Version++;

        if (_spare.Count < MaxSpare)
        {
            _spare.Push(item);
        }
    }

    private bool IsRunning { get; set; }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => RunAsync(stoppingToken), stoppingToken);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        IsRunning = true;

        using var timer = new PeriodicTimer(Period, _time);

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
    /// One tick: the actions that are due run, then every simulation steps. Public for tests, which drive the clock by hand.
    /// </summary>
    public void Tick()
    {
        // The step is measured on the clock the game time is read from, so a test that moves the time by hand decides how
        // long a step takes too.
        var now = _time.GetTimestamp();
        var elapsed = _time.GetElapsedTime(_last, now);

        _last = now;

        if (elapsed > MaxStep)
        {
            elapsed = MaxStep;
        }

        // The game time moves by whole periods, so a delay of one period is the next tick even when the timer of the
        // process fires a little early.
        var periods = Math.Clamp((long)Math.Round(elapsed / Period), 1, MaxStep / Period);

        Now += Period * periods;

        RunDue();
        Step(elapsed);

        Measure(_time.GetElapsedTime(now));
    }

    private void RunDue()
    {
        var now = Now.Ticks;

        // An action that schedules another puts it past this tick, so the loop ends.
        while (_queue.TryPeek(out var item, out var priority) && priority.Due <= now)
        {
            _queue.Dequeue();

            var action = item.Action;

            if (action is null)
            {
                _cancelled--;

                Recycle(item);

                continue;
            }

            item.Group?.Remove(item);
            item.Action = null;
            item.Group = null;

            Recycle(item);

            try
            {
                action();
            }

            catch (Exception e)
            {
                LogActionFailed(e);
            }
        }
    }

    private void Step(TimeSpan elapsed)
    {
        // A step may add or remove simulations, its own included: the list is walked in place.
        var count = _simulations.BeginStep();

        try
        {
            for (var index = 0; index < count; index++)
            {
                if (_simulations.At(index) is not { } simulation)
                {
                    continue;
                }

                try
                {
                    simulation.Advance(elapsed);
                }

                catch (Exception e)
                {
                    LogStepFailed(e, simulation.GetType().Name);
                }
            }
        }

        finally
        {
            _simulations.EndStep();
        }
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

        if (_time.GetElapsedTime(_lastReport) < ReportInterval)
        {
            return;
        }

        LogOverBudget(_overBudget, Period.TotalMilliseconds, ReportInterval.TotalSeconds, _worst.TotalMilliseconds, _simulations.Count);

        _overBudget = 0;
        _worst = TimeSpan.Zero;
        _lastReport = _time.GetTimestamp();
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "An action scheduled on the simulation clock failed.")]
    private partial void LogActionFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Simulation {Simulation} failed to step.")]
    private partial void LogStepFailed(Exception exception, string simulation);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "{Count} steps took longer than {Budget} ms in the last {Interval} s, the worst {Worst} ms, with {Simulations} simulations.")]
    private partial void LogOverBudget(int count, double budget, double interval, double worst, int simulations);
}
