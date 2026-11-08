using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenWorker.Hotspot;

namespace OpenWorker.Gameplay.Modules.State;

/// <summary>
/// Lets the features that keep time for their persons do their work every so often, on the world loop.
/// </summary>
public sealed partial class MiscTickService(WorldLoop loop, IEnumerable<IMiscTick> ticks, ILogger<MiscTickService> logger) : BackgroundService
{
    private static TimeSpan Frequency { get; } = TimeSpan.FromSeconds(30);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => LoopAsync(stoppingToken), stoppingToken);
    }

    private async Task LoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(Frequency);

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            foreach (var tick in ticks)
            {
                try
                {
                    await loop.InvokeAsync(() => tick.TickAsync(cancellationToken));
                }

                catch (Exception e) when (e is not OperationCanceledException)
                {
                    LogTickFailed(e, tick.GetType().Name);
                }
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "{Tick} failed.")]
    private partial void LogTickFailed(Exception exception, string tick);
}
