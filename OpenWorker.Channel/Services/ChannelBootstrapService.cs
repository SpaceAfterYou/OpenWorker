using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OpenWorker.Extensions;
using OpenWorker.Hotspot.Cache.Types;
using Redis.OM.Searching;

namespace OpenWorker.Channel.Services;

internal sealed class ChannelBootstrapService(
    IRedisCollection<ChannelCache> cache,
    ServiceChannels channels,
    IConfiguration configuration
) :
    BackgroundService
{
    private short Location { get; } = configuration.GetLocation();
    
    private short Gate { get; } = configuration.GetGate();
    
    private Guid Instance { get; } = configuration.GetInstance();

#region BackgroundService
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var value in channels.Select(CreateChannelCache))
        {
            await cache
                .InsertAsync(value)
                .ConfigureAwait(false);
        }
    }
    
#endregion BackgroundService
    
    private ChannelCache CreateChannelCache(ServiceChannel channel)
    {
        return new ChannelCache
        {
            Owner = Instance,
            Gate = Gate,
            Location = Location,
            Identifier = channel.Identifier,
            OnlineCount = channel.Online
        };
    }
}