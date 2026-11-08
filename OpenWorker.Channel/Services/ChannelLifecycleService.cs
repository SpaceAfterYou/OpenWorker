using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenWorker.Extensions;
using OpenWorker.Hotspot.Cache.Types;
using Redis.OM.Searching;

namespace OpenWorker.Channel.Services;

internal sealed partial class ChannelLifecycleService(
    IRedisCollection<ChannelCache> cache,
    ServiceChannels channels,
    IOptions<InstanceOptions> instance,
    ILogger<ChannelLifecycleService> logger
) :
    BackgroundService
{
    private Guid Instance { get; } = instance.Value.Identifier;

    private static TimeSpan Frequency { get; } = TimeSpan.FromSeconds(1);

#region BackgroundService

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Frequency);

        try
        {
            do
            {
                try
                {
                    await UpdateAsync();
                }

                catch (Exception e) when (e is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
                {
                    LogUpdateFailed(logger, e);
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        catch (OperationCanceledException)
        {
            // The server is stopping.
        }
    }

#endregion BackgroundService

    private async Task UpdateAsync()
    {
        foreach (var channel in channels)
        {
            var value = await cache
                .FirstOrDefaultAsync(e => e.Identifier == channel.Identifier && e.Owner == Instance);

            if (value is null)
            {
                LogChannelNotFound(logger, Instance, channel.Identifier);
                continue;
            }

            value.OnlineCount = channel.Online;

            await cache
                .UpdateAsync(value);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Channel not found: Owner={Owner}, Identifier={Identifier}")]
    private static partial void LogChannelNotFound(ILogger logger, Guid owner, short identifier);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to update the online count of the channels.")]
    private static partial void LogUpdateFailed(ILogger logger, Exception exception);
}
