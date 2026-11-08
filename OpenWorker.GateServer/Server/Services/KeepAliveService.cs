using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Modules.Channels.Enums;
using Redis.OM;
using Redis.OM.Searching;

namespace OpenWorker.GateServer.Server.Services;

/// <summary>
/// Tells the login server that this gate runs, how busy it is and where clients reach it, every second. When the server
/// drains the record is removed, and the login server stops sending players to a gate that is going away.
/// </summary>
public sealed partial class KeepAliveService(
    IRedisCollection<ChannelCache> channelCollection,
    IRedisCollection<GateCache> gateCollection,
    IOptions<GateOptions> gate,
    IOptions<GateEndpointOptions> endpoint,
    ILogger<KeepAliveService> logger
) :
    RegistrationService
{
    private GateCache Instance { get; set; } = new()
    {
        Identifier = gate.Value.Id,

        OnlineCount = 0,
        Workload = GateWorkload.Low,

        Host = endpoint.Value.Host,
        Port = (short)endpoint.Value.Port
    };

    private static TimeSpan Frequency { get; } = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Frequency);

        try
        {
            do
            {
                try
                {
                    await UpdateAsync(stoppingToken);
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
    /// Runs after the update loop has ended, so no update can write the record again.
    /// </summary>
    protected override async Task RemoveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await gateCollection
                .DeleteAsync(Instance);
        }

        catch (Exception e) when (e is not OperationCanceledException)
        {
            LogRemoveFailed(logger, e);
        }
    }

    private async Task UpdateAsync(CancellationToken cancellationToken)
    {
        var channels = await channelCollection
            .Where(x => x.Gate == Instance.Identifier)
            .ToArrayAsync(cancellationToken);

        Instance = Instance with
        {
            OnlineCount = (int)channels.Sum(x => x.OnlineCount),
            Workload = GetWorkload(channels),
            UpdatedAt = DateTime.UtcNow,
        };

        await gateCollection
            .InsertAsync(Instance);
    }

    private static GateWorkload GetWorkload(ChannelCache[] values)
    {
        // If district server is offline
        // There will be no channels in the group
        
        if (values.Length == 0)
        {
            return GateWorkload.Offline;
        }
        
        return ChannelUtils.GetWorkload(values.Average(x => x.OnlineCount)) switch
        {
            ChannelWorkload.Low => GateWorkload.Low,
            ChannelWorkload.Normal => GateWorkload.Normal,
            ChannelWorkload.High => GateWorkload.Full,
            ChannelWorkload.Full => GateWorkload.Busy,

            _ => GateWorkload.Offline
        };
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to remove the gate record from Redis, the login server takes the gate for offline when the record is a minute old.")]
    private static partial void LogRemoveFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to update the gate record.")]
    private static partial void LogUpdateFailed(ILogger logger, Exception exception);
}
