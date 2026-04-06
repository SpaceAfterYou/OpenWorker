using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenWorker.Extensions;
using OpenWorker.Hotspot.Cache.Types;
using Redis.OM.Searching;

namespace OpenWorker.Channel.Services;

internal sealed class ChannelLifecycleService(
    IRedisCollection<ChannelCache> cache,
    ServiceChannels channels,
    IConfiguration configuration,
    ILogger<ChannelLifecycleService> logger
) :
    BackgroundService
{
    private Guid Instance { get; } = configuration.GetInstance();

#region BackgroundService

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _ = Task.Factory.StartNew(
            () => OnTickAsync(cancellationToken).ConfigureAwait(false),
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default
        );

        return Task.CompletedTask;
    }

#endregion BackgroundService

    private async ValueTask OnTickAsync(CancellationToken cancellationToken)
    {
        while (cancellationToken.IsCancellationRequested is false)
        {
            foreach (var channel in channels)
            {
                var value = await cache
                    .FirstOrDefaultAsync(e => e.Identifier == channel.Identifier && e.Owner == Instance)
                    .ConfigureAwait(false);

                if (value is null)
                {
                    logger.LogDebug("Channel not found: Owner={Guid}, Identifier={Identifier}", Instance, channel.Identifier);
                    continue;
                }

                value.OnlineCount = channel.Online;

                await cache
                    .UpdateAsync(value)
                    .ConfigureAwait(false);

                await Task
                    .Delay(TimeSpan.FromSeconds(1), cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}
