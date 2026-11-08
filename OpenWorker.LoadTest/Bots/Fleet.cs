using System.Diagnostics;
using OpenWorker.LoadTest.Cli;

namespace OpenWorker.LoadTest.Bots;

/// <summary>
/// The part of a run that the bots go through together.
/// </summary>
internal enum RunPhase
{
    /// <summary>Bots are being started at the rate asked for.</summary>
    RampUp,

    /// <summary>Every bot has started; the last ones are still getting in, and then the bots play unmeasured.</summary>
    WarmUp,

    /// <summary>The latency is recorded.</summary>
    Measuring,

    /// <summary>The bots are told to leave.</summary>
    Stopping,

    Done
}

/// <summary>
/// Starts the bots at the pace of the ramp-up, lets them settle, measures for the time asked for, and stops them.
/// </summary>
internal sealed class Fleet(RunContext context)
{
    private static readonly TimeSpan StopGrace = TimeSpan.FromSeconds(10);

    private volatile RunPhase _phase = RunPhase.RampUp;

    public RunPhase Phase => _phase;

    /// <summary>
    /// Whether the run was cut short from outside (Ctrl+C) before it had measured for the time asked for.
    /// </summary>
    public bool Interrupted { get; private set; }

    /// <summary>
    /// Runs the bots. Returns when they have all left, whether the run completed or was stopped.
    /// </summary>
    public async Task RunAsync(CancellationToken stopping)
    {
        var options = context.Options;
        var stats = context.Stats;

        using var finish = CancellationTokenSource.CreateLinkedTokenSource(stopping);

        var bots = new List<Task>(options.Bots);

        try
        {
            await StartAsync(bots, finish.Token);
            await SettleAsync(finish.Token);

            _phase = RunPhase.WarmUp;

            await Task.Delay(options.Warmup, finish.Token);

            if (stats.Active == 0)
            {
                // Nobody got in, so there is nothing to measure; the errors say why.
                return;
            }

            stats.StartMeasuring();
            _phase = RunPhase.Measuring;

            await Task.Delay(options.Duration, finish.Token);
        }

        catch (OperationCanceledException) when (stopping.IsCancellationRequested)
        {
            Interrupted = true;
        }

        finally
        {
            stats.StopMeasuring();
            _phase = RunPhase.Stopping;

            await finish.CancelAsync();

            try
            {
                await Task.WhenAll(bots).WaitAsync(StopGrace, CancellationToken.None);
            }

            catch (TimeoutException)
            {
                // A bot that does not stop in time is left to the end of the process.
            }

            _phase = RunPhase.Done;
        }
    }

    private async Task StartAsync(List<Task> bots, CancellationToken cancellation)
    {
        var options = context.Options;
        var begin = Stopwatch.GetTimestamp();

        for (var i = 0; i < options.Bots; i++)
        {
            var due = begin + (long)(i / options.RampUp * Stopwatch.Frequency);
            var wait = due - Stopwatch.GetTimestamp();

            // Waits for a millisecond or more; a smaller gap is not worth a timer, and the next start catches up.
            if (wait > Stopwatch.Frequency / 1000)
            {
                await Task.Delay(Stopwatch.GetElapsedTime(0, wait), cancellation);
            }

            cancellation.ThrowIfCancellationRequested();

            var bot = new Bot(i, context);

            bots.Add(Task.Run(() => bot.RunAsync(cancellation), CancellationToken.None));
        }
    }

    /// <summary>
    /// Waits until the bots that started are in or have failed, within a limit that allows each step its timeout.
    /// </summary>
    private async Task SettleAsync(CancellationToken cancellation)
    {
        var limit = (context.Options.StageTimeout * 6) + TimeSpan.FromSeconds(30);
        var begin = Stopwatch.GetTimestamp();

        while (context.Stats.Entering > 0 && Stopwatch.GetElapsedTime(begin) < limit)
        {
            await Task.Delay(100, cancellation);
        }
    }
}
