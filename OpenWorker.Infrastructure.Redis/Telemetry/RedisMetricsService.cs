using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OpenWorker.Hotspot.Telemetry;

/// <summary>
/// Turns the commands the profiler of the Redis connection has collected into measurements of <c>redis.call.duration</c>.
/// </summary>
internal sealed partial class RedisMetricsService(RedisMetrics metrics, TimeProvider time, ILogger<RedisMetricsService> logger) : BackgroundService
{
    /// <summary>
    /// How long a command waits in the profiler before it is recorded. The measurement is the time of the command, not of the flush.
    /// </summary>
    public static TimeSpan Interval { get; } = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, time);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                Flush();
            }
        }

        catch (OperationCanceledException)
        {
            // The server is stopping.
        }

        // The commands of the last second, and the ones that closed the server down, are recorded too.
        Flush();
    }

    private void Flush()
    {
        try
        {
            metrics.Flush();
        }

        catch (Exception e)
        {
            LogFlushFailed(logger, e);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Could not record the Redis calls.")]
    private static partial void LogFlushFailed(ILogger logger, Exception exception);
}
