using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenWorker.Hotspot;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// Delivers the pending letters of rewards (<see cref="InventoryRewardGranter.DeliverPendingAsync"/>), on the world loop: at the
/// start all of them, as the server that wrote them has stopped, then every so often those that their server has had time to
/// deliver itself. Several servers may run it at once: each letter is delivered once.
/// </summary>
public sealed partial class RewardLetterRecovery(
    WorldLoop loop,
    InventoryRewardGranter granter,
    TimeProvider time,
    ILogger<RewardLetterRecovery> logger) : BackgroundService
{
    public static TimeSpan Interval { get; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// How long a running server is left to deliver a letter it has kept, by its own tries.
    /// </summary>
    public static TimeSpan Grace { get; } = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, time);

        var writtenBefore = time.GetUtcNow().UtcDateTime;

        try
        {
            do
            {
                try
                {
                    var before = writtenBefore;

                    await loop.InvokeAsync(() => DeliverAsync(before, stoppingToken));
                }

                catch (Exception e) when (e is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
                {
                    LogFailed(logger, e);
                }

                writtenBefore = time.GetUtcNow().UtcDateTime - Grace;
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        catch (OperationCanceledException)
        {
            // The server is stopping.
        }
    }

    private async ValueTask DeliverAsync(DateTime writtenBefore, CancellationToken cancellationToken)
    {
        var delivered = await granter.DeliverPendingAsync(writtenBefore, cancellationToken);

        if (delivered > 0)
        {
            LogDelivered(logger, delivered);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Delivered {Count} pending letters of rewards")]
    private static partial void LogDelivered(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "Delivering the pending letters of rewards failed; the next round tries again")]
    private static partial void LogFailed(ILogger logger, Exception exception);
}
