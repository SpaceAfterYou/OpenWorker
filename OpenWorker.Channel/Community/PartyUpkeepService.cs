using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenWorker.Hotspot;

namespace OpenWorker.Channel.Community;

/// <summary>
/// A part of the parties that has to act in time on its own: the auto matching moves its groups on, the board takes down
/// what expired.
/// </summary>
public interface IPartyClock
{
    Task TickAsync();
}

/// <summary>
/// Runs the clocks of the parties on the world loop every second, and every <see cref="CommunityNetwork.PresenceRenewal"/>
/// keeps the parties of the people of this server alive and lets go of the members gone for good. A round that fails is
/// logged and the next one runs.
/// </summary>
public sealed partial class PartyUpkeepService(
    PartyNotifier notifier,
    IEnumerable<IPartyClock> clocks,
    WorldLoop loop,
    TimeProvider time,
    ILogger<PartyUpkeepService> logger
) :
    BackgroundService
{
    public static TimeSpan Period { get; } = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Period, time);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await loop.InvokeAsync(RoundAsync);
                }

                catch (Exception e) when (e is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
                {
                    LogRoundFailed(logger, e);
                }
            }
        }

        catch (OperationCanceledException)
        {
            // The server is stopping.
        }
    }

    private long _rounds;

    private async ValueTask RoundAsync()
    {
        foreach (var clock in clocks)
        {
            await clock.TickAsync();
        }

        if (++_rounds % (long)(CommunityNetwork.PresenceRenewal / Period) == 0)
        {
            await notifier.UpkeepAsync();
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "A round of the party upkeep failed.")]
    private static partial void LogRoundFailed(ILogger logger, Exception exception);
}
