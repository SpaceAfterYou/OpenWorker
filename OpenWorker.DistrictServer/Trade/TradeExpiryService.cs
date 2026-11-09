using Arch.Core;
using OpenWorker.Hotspot;

namespace OpenWorker.DistrictServer.Trade;

/// <summary>
/// Every second, on the world loop, cancels the trade invitations nobody answered in time (<see cref="Trades.Expire"/>).
/// </summary>
public sealed partial class TradeExpiryService(
    WorldLoop loop,
    Trades trades,
    TimeProvider time,
    ILogger<TradeExpiryService> logger) : BackgroundService
{
    public static TimeSpan Frequency { get; } = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Frequency, time);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await loop.InvokeAsync(trades.Expire);
                }

                catch (Exception e) when (e is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
                {
                    LogExpiryFailed(logger, e);
                }
            }
        }

        catch (OperationCanceledException)
        {
            // The server is stopping.
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "The expiry of trade invitations failed.")]
    private static partial void LogExpiryFailed(ILogger logger, Exception exception);
}

/// <summary>
/// The connection of a player closes: its trade is cancelled before anything of the player is written, so no exchange can
/// touch the player after its last snapshot.
/// </summary>
public sealed class TradeCloseHandler(Trades trades) : ISessionCloseHandler
{
    public ValueTask OnSessionClosedAsync(Entity player)
    {
        trades.Leave(player);

        return ValueTask.CompletedTask;
    }
}
