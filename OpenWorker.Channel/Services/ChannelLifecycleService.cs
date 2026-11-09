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
/// <see cref="ChannelBootstrapService"/> with a lifetime, renewed here, and taken back here when the server drains: a
/// channel that stays in Redis after its server has gone sends the players of the next login to a server that is not
/// there, and the lifetime makes the records of a server that crashed go by themselves.<br/>
/// The count is written when it changes, not on a clock: a join or a leave wakes the service, which waits
/// <see cref="Debounce"/> for the joins and leaves that follow and then writes each channel whose count differs from
/// what it wrote last, one write per channel. Every <see cref="Renewal"/> it writes every record, changed or not, which
/// renews its <see cref="Lifetime"/> and writes again one that expired while Redis was away.
/// </summary>
internal sealed partial class ChannelLifecycleService(
    IRedisCollection<ChannelCache> cache,
    ServiceChannels channels,
    IOptions<InstanceOptions> instance,
    IOptions<GateOptions> gate,
    TimeProvider time,
    ILogger<ChannelLifecycleService> logger
) :
    RegistrationService
{
    /// <summary>
    /// How long the joins and leaves after the first are gathered before the counts are written.
    /// </summary>
    public static TimeSpan Debounce { get; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How often every record is written again with its lifetime, whether its count changed or not.
    /// </summary>
    public static TimeSpan Renewal { get; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How long the record of a channel lives without a write.
    /// </summary>
    internal static TimeSpan Lifetime { get; } = TimeSpan.FromSeconds(30);

    private Guid Instance { get; } = instance.Value.Identifier;

    private short Location { get; } = instance.Value.Location;

    private short Gate { get; } = gate.Value.Id;

    // What the record of each channel holds, as far as this service knows: the bootstrap writes 0.
    private readonly short[] _written = new short[channels.Count];

#region BackgroundService

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // The bootstrap has just written every record with its lifetime.
        var renewed = time.GetTimestamp();

        try
        {
            while (true)
            {
                var changed = await WaitForChangeAsync(Renewal - time.GetElapsedTime(renewed), stoppingToken);

                if (changed)
                {
                    await Task.Delay(Debounce, time, stoppingToken);
                }

                var renew = time.GetElapsedTime(renewed) >= Renewal;

                try
                {
                    await PublishAsync(renew, stoppingToken);

                    if (renew)
                    {
                        renewed = time.GetTimestamp();
                    }
                }

                catch (Exception e) when (e is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
                {
                    LogUpdateFailed(logger, e);

                    // What was not written is tried again after the next wait.
                    channels.Changes.Notify();
                }
            }
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

    /// <summary>
    /// Waits for a join or a leave, or until the next renewal is due.
    /// </summary>
    /// <returns>Whether a change came.</returns>
    private async Task<bool> WaitForChangeAsync(TimeSpan due, CancellationToken stoppingToken)
    {
        if (due <= TimeSpan.Zero)
        {
            return false;
        }

        using var timeout = new CancellationTokenSource(due, time);
        using var wait = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeout.Token);

        try
        {
            await channels.Changes.WaitAsync(wait.Token);

            return true;
        }

        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
            return false;
        }
    }

    /// <param name="renew">Write every record, not only those whose count changed.</param>
    private async Task PublishAsync(bool renew, CancellationToken cancellationToken)
    {
        for (var index = 0; index < channels.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var channel = channels[index];
            var online = channel.Online;

            if (!renew && online == _written[index])
            {
                continue;
            }

            // The whole record with its lifetime in one write: it renews the record, or writes again one that expired.
            await cache.InsertAsync(new ChannelCache
            {
                Guid = channel.Record,
                Owner = Instance,
                Gate = Gate,
                Location = Location,
                Identifier = channel.Identifier,
                OnlineCount = online
            }, Lifetime);

            _written[index] = online;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to remove the channels of the server from Redis.")]
    private static partial void LogRemoveFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to update the online count of the channels.")]
    private static partial void LogUpdateFailed(ILogger logger, Exception exception);
}
