using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Stores;

namespace OpenWorker.Gameplay.Modules.CashShop;

/// <summary>
/// Clears the counts of limited products whose period is over. A count does not keep its kind, so one is swept once its period
/// started before the running week and the running month both; the counts for good stay. It runs off the world loop, in
/// batches of one statement each, so several servers may run it at once.
/// </summary>
public sealed partial class CashShopSweepService(ICashShopStore store, TimeProvider time, ILogger<CashShopSweepService> logger)
    : BackgroundService
{
    /// <summary>
    /// How often the counts are swept.
    /// </summary>
    public static TimeSpan Interval { get; } = TimeSpan.FromHours(1);

    /// <summary>
    /// The most counts one statement deletes.
    /// </summary>
    public const int Batch = 500;

    /// <summary>
    /// The most batches of one round; what is left waits for the next round.
    /// </summary>
    public const int MaxBatches = 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, time);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
            {
                LogSweepFailed(logger, exception);
            }
        }
    }

    /// <summary>
    /// The earliest start of a period that still runs: a count that started before it belongs to a period that is over,
    /// whatever its kind.
    /// </summary>
    public static DateTime OldestRunning(DateTimeOffset now)
    {
        var week = CashLimits.PeriodOf(CashLimitKind.CharacterWeek, now);
        var month = CashLimits.PeriodOf(CashLimitKind.CharacterMonth, now);

        return week < month ? week : month;
    }

    /// <summary>
    /// One round: batch after batch until a batch comes back short or the round has done its share.
    /// </summary>
    public async Task<int> SweepAsync(CancellationToken cancellationToken)
    {
        var before = OldestRunning(time.GetLocalNow());
        var total = 0;

        for (var i = 0; i < MaxBatches; i++)
        {
            var swept = await store.SweepPurchasesAsync(CashLimits.Always, before, Batch, cancellationToken);

            total += swept;

            if (swept < Batch)
            {
                break;
            }
        }

        if (total > 0)
        {
            LogSwept(logger, total);
        }

        return total;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Swept {Count} counts of limited cash shop products whose period was over")]
    private static partial void LogSwept(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "Sweeping the counts of the cash shop failed; the next round tries again")]
    private static partial void LogSweepFailed(ILogger logger, Exception exception);
}
