using System.Diagnostics;

namespace OpenWorker.LoadTest.Monitoring;

/// <summary>
/// One reading of the bot process.
/// </summary>
/// <param name="Seconds">Since the start of the run.</param>
/// <param name="CpuCores">Processor time used since the last reading, in cores: 1.5 is one and a half cores busy.</param>
internal sealed record SelfSample(
    double Seconds,
    double CpuCores,
    long WorkingSetBytes,
    long GcGen0,
    long GcGen1,
    long GcGen2,
    double GcPauseMilliseconds,
    int Threads,
    long PendingWork);

/// <summary>
/// Watches the bot process itself. A run of thousands of bots can ask more of one process than it has to give, and
/// then the latency it reports is partly its own: the figures here tell whether it was.
/// </summary>
internal sealed class SelfSampler
{
    private readonly List<SelfSample> _samples = [];
    private readonly Lock _lock = new();

    public IReadOnlyList<SelfSample> Samples
    {
        get
        {
            lock (_lock)
            {
                return [.. _samples];
            }
        }
    }

    public async Task RunAsync(TimeSpan interval, long startTimestamp, CancellationToken cancellation)
    {
        using var process = Process.GetCurrentProcess();
        using var timer = new PeriodicTimer(interval);

        var lastTime = Stopwatch.GetTimestamp();
        var lastCpu = process.TotalProcessorTime;

        try
        {
            while (await timer.WaitForNextTickAsync(cancellation))
            {
                var now = Stopwatch.GetTimestamp();

                process.Refresh();

                var cpu = process.TotalProcessorTime;
                var elapsed = Stopwatch.GetElapsedTime(lastTime, now).TotalSeconds;

                var sample = new SelfSample(
                    Stopwatch.GetElapsedTime(startTimestamp, now).TotalSeconds,
                    elapsed > 0 ? (cpu - lastCpu).TotalSeconds / elapsed : 0,
                    process.WorkingSet64,
                    GC.CollectionCount(0),
                    GC.CollectionCount(1),
                    GC.CollectionCount(2),
                    GC.GetTotalPauseDuration().TotalMilliseconds,
                    ThreadPool.ThreadCount,
                    ThreadPool.PendingWorkItemCount);

                lock (_lock)
                {
                    _samples.Add(sample);
                }

                lastTime = now;
                lastCpu = cpu;
            }
        }

        catch (OperationCanceledException)
        {
            // The run is over.
        }
    }
}
