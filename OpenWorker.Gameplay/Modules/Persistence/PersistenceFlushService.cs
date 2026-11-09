using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Hotspot;

namespace OpenWorker.Gameplay.Modules.Persistence;

/// <summary>
/// Every <c>Persistence:FlushInterval</c> takes what the players of the server changed, on the world loop, and hands it to
/// <see cref="PersistenceWriter"/> in one batch. A crash loses at most one interval. When the host stops, after the drain
/// has closed the sessions (each of them writes as it closes), one last round takes what is left.
/// </summary>
public sealed partial class PersistenceFlushService(
    WorldLoop loop,
    PersonSnapshots snapshots,
    PersistenceWriter writer,
    DatabaseItemSerials serials,
    IOptions<PersistenceOptions> options,
    TimeProvider time,
    ILogger<PersistenceFlushService> logger) : BackgroundService
{
    private TimeSpan Interval { get; } = options.Value.FlushInterval;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await serials.PrimeAsync();
        }

        catch (Exception exception)
        {
            LogSerialsNotReserved(logger, exception);
        }

        using var timer = new PeriodicTimer(Interval, time);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await FlushAsync();
                }

                catch (Exception exception)
                {
                    LogFlushFailed(logger, exception);
                }
            }
        }

        catch (OperationCanceledException)
        {
            // The server is stopping; StopAsync takes the last round.
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        try
        {
            await FlushAsync().WaitAsync(cancellationToken);
        }

        catch (Exception exception)
        {
            LogLastFlushFailed(logger, exception);
        }
    }

    /// <summary>
    /// One round: the snapshots and the queue on the loop, the write off it. Returns when the round is written.
    /// </summary>
    public async Task<bool> FlushAsync()
    {
        var written = await await loop.InvokeAsync(() =>
        {
            var writes = snapshots.TakeAll();

            return writes.Count == 0 ? Task.FromResult(true) : writer.EnqueueAsync(writes);
        });

        return written;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Item serials could not be reserved yet")]
    private static partial void LogSerialsNotReserved(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Flushing the changes of the players failed")]
    private static partial void LogFlushFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "The last flush of the changes of the players failed")]
    private static partial void LogLastFlushFailed(ILogger logger, Exception exception);
}
