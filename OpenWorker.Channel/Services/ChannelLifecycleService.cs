using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache.Types;
using Redis.OM;
using Redis.OM.Searching;

namespace OpenWorker.Channel.Services;

/// <summary>
/// Keeps the online count of the channels of this server in Redis. The records themselves are written by
/// <see cref="ChannelBootstrapService"/> with a lifetime that every update renews, and taken back here when the server
/// drains: a channel that stays in Redis after its server has gone sends the players of the next login to a server that
/// is not there, and the lifetime makes the records of a server that crashed go by themselves.
/// </summary>
internal sealed partial class ChannelLifecycleService(
    IRedisCollection<ChannelCache> cache,
    ServiceChannels channels,
    IOptions<InstanceOptions> instance,
    IOptions<GateOptions> gate,
    ILogger<ChannelLifecycleService> logger
) :
    RegistrationService
{
    private Guid Instance { get; } = instance.Value.Identifier;
    private short Location { get; } = instance.Value.Location;
    private short Gate { get; } = gate.Value.Id;

    private static TimeSpan Frequency { get; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How long the record of a channel lives without an update.
    /// </summary>
    internal static TimeSpan Lifetime { get; } = TimeSpan.FromSeconds(30);

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

    /// <summary>
    /// Removes every channel of this server. Runs after the update loop has ended, so no update can bring one back.
    /// </summary>
    protected override async Task RemoveAsync(CancellationToken cancellationToken)
    {
        try
        {
            var records = await cache
                .Where(e => e.Owner == Instance)
                .ToArrayAsync(cancellationToken);

            await cache
                .DeleteAsync(records);
        }

        catch (Exception e) when (e is not OperationCanceledException)
        {
            LogRemoveFailed(logger, e);
        }
    }

#endregion BackgroundService

    private async Task UpdateAsync()
    {
        foreach (var channel in channels)
        {
            var value = await cache
                .FirstOrDefaultAsync(e => e.Identifier == channel.Identifier && e.Owner == Instance);

            // The record expired (Redis was away longer than its lifetime): it is written again.
            if (value is null)
            {
                LogChannelNotFound(logger, Instance, channel.Identifier);

                await cache.InsertAsync(new ChannelCache
                {
                    Owner = Instance,
                    Gate = Gate,
                    Location = Location,
                    Identifier = channel.Identifier,
                    OnlineCount = channel.Online
                }, Lifetime);

                continue;
            }

            value.OnlineCount = channel.Online;

            // Written whole with its lifetime: the update renews the record.
            await cache
                .InsertAsync(value, Lifetime);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Channel not found: Owner={Owner}, Identifier={Identifier}")]
    private static partial void LogChannelNotFound(ILogger logger, Guid owner, short identifier);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to remove the channels of the server from Redis.")]
    private static partial void LogRemoveFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to update the online count of the channels.")]
    private static partial void LogUpdateFailed(ILogger logger, Exception exception);
}
