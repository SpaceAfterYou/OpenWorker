using Arch.Core;
using OpenWorker.Channel;
using OpenWorker.DistrictServer.Server.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Channels;
using OpenWorker.Hotspot.Modules.System.Responses;

namespace OpenWorker.DistrictServer.Server.Services;

public sealed class KeepAliveService(World world, ServiceChannels channelCollection) : BackgroundService
{
    private static TimeSpan Frequency { get; } = TimeSpan.FromMinutes(1);

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.Factory.StartNew(
            () => LoopAsync(cancellationToken).ConfigureAwait(false),
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default
        );
    }

    private async Task LoopAsync(CancellationToken cancellationToken)
    {
        var stopWatch = new System.Diagnostics.Stopwatch();

        while (cancellationToken.IsCancellationRequested is false)
        {
            var ticks = TimeSpan.FromMilliseconds(DateTime.UtcNow.Ticks) - Frequency;

            stopWatch.Start();

            foreach (var channel in channelCollection)
            {
                channel.ForEach(entity =>
                {
                    var component = world.Get<KeepAliveComponent>(entity);

                    if (component.LastTickCount >= ticks)
                    {
                        return;
                    }

                    var session = world.Get<ServerSessionComponent>(entity);
                    
                    var penalty = (byte)(1 + component.PenaltyCount);
                    
                    if (penalty > 5)
                    {
                        session.Disconnect();
                        return;
                    }

                    session.Send(new SystemKeepAliveResponse { Time = component.LastTickCount });
                    
                    world.Set(entity, component with { PenaltyCount = penalty });   
                });
            }

            stopWatch.Stop();

            await Task
                .Delay(Frequency - stopWatch.Elapsed, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}