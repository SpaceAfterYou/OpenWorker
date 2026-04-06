using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Modules.Channels.Enums;
using Redis.OM;
using Redis.OM.Searching;

namespace OpenWorker.GateServer.Server.Services;

public sealed class KeepAliveService(
    IRedisCollection<ChannelCache> channelCollection,
    IRedisCollection<GateCache> gateCollection,
    IConfiguration configuration
) :
    BackgroundService
{
    private GateCache Instance { get; set; } = new()
    {
        Identifier = configuration.GetGate(),

        OnlineCount = 0,
        Workload = GateWorkload.Low,

        Host = configuration.GetGateHost(),
        Port = configuration.GetGatePort()
    };

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _ = Task.Factory.StartNew(
            () => OnTickAsync(cancellationToken).ConfigureAwait(false),
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default
        ).ConfigureAwait(false);
        
        return Task.CompletedTask;
    }

    private async Task OnTickAsync(CancellationToken cancellationToken)
    {
        await gateCollection
            .InsertAsync(Instance)
            .ConfigureAwait(false);
        
        while (cancellationToken.IsCancellationRequested is false)
        {
            var channels = await channelCollection
                .Where(x => x.Gate == Instance.Identifier)
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

            Instance = Instance with
            {
                OnlineCount = (int)channels.Sum(x => x.OnlineCount),
                Workload = GetWorkload(channels),
                UpdatedAt = DateTime.UtcNow,
            };
            
            await gateCollection
                .InsertAsync(Instance)
                .ConfigureAwait(false);

            await Task
                .Delay(TimeSpan.FromSeconds(1), cancellationToken)
                .ConfigureAwait(false);
        }
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
}