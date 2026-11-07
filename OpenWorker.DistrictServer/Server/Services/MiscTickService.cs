using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot;

namespace OpenWorker.DistrictServer.Server.Services;

/// <summary>
/// Lets the features that keep time for their persons do their work every so often.
/// </summary>
public sealed class MiscTickService(WorldLoop loop, IEnumerable<IMiscTick> ticks, ILogger<MiscTickService> logger) : BackgroundService
{
    private static TimeSpan Frequency { get; } = TimeSpan.FromSeconds(30);

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() => LoopAsync(cancellationToken), cancellationToken);
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
                    logger.LogError(e, "{Tick} failed.", tick.GetType().Name);
                }
            }
        }
    }
}
