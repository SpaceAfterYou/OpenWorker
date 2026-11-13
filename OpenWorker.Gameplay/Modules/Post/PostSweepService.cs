using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Stores;

namespace OpenWorker.Gameplay.Modules.Post;

/// <summary>
/// Clears the letters whose time is up from the database, with what they still carry, as the game loses them. The boxes
/// already pass over such letters whether the sweep has run or not; the sweep keeps the table from growing. It runs off the
/// world loop and touches no player, in batches of one statement each, so several servers may run it at once.
/// </summary>
public sealed partial class PostSweepService(IPostStore store, TimeProvider time, ILogger<PostSweepService> logger) : BackgroundService
{
    /// <summary>
    /// How often the letters are swept.
    /// </summary>
    public static TimeSpan Interval { get; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// The most letters one statement deletes.
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
    /// One round: batch after batch until a batch comes back short or the round has done its share.
    /// </summary>
    public async Task<int> SweepAsync(CancellationToken cancellationToken)
    {
        var total = 0;

        for (var i = 0; i < MaxBatches; i++)
        {
            var swept = await store.SweepAsync(Batch, cancellationToken);

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

    [LoggerMessage(Level = LogLevel.Information, Message = "Swept {Count} letters whose time was up")]
    private static partial void LogSwept(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "Sweeping the letters whose time was up failed; the next round tries again")]
    private static partial void LogSweepFailed(ILogger logger, Exception exception);
}
