using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Modules.Channels.Enums;
using Redis.OM;
using Redis.OM.Searching;

namespace OpenWorker.GateServer.Server.Services;

public sealed partial class KeepAliveService(
    IRedisCollection<ChannelCache> channelCollection,
    IRedisCollection<GateCache> gateCollection,
    IOptions<GateOptions> gate,
    IOptions<GateEndpointOptions> endpoint,
    ILogger<KeepAliveService> logger
) :
    BackgroundService
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

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to update the gate record.")]
    private static partial void LogUpdateFailed(ILogger logger, Exception exception);
}
