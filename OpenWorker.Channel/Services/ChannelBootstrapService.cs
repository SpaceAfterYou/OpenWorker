using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenWorker.Extensions;
using OpenWorker.Hotspot.Cache.Types;
using Redis.OM.Searching;

namespace OpenWorker.Channel.Services;

internal sealed class ChannelBootstrapService(
    IRedisCollection<ChannelCache> cache,
    ServiceChannels channels,
    IOptions<InstanceOptions> instance,
    IOptions<GateOptions> gate
) :
    BackgroundService
{
    private short Location { get; } = instance.Value.Location;

    private short Gate { get; } = gate.Value.Id;

    private Guid Instance { get; } = instance.Value.Identifier;

#region BackgroundService

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // With a lifetime: ChannelLifecycleService renews the records, and those of a server that crashed expire.
        foreach (var channel in channels)
        {
            await cache.InsertAsync(CreateChannelCache(channel), ChannelLifecycleService.Lifetime);
        }
    }

#endregion BackgroundService

    private ChannelCache CreateChannelCache(ServiceChannel channel) => new()
    {
        Owner = Instance,
        Gate = Gate,
        Location = Location,
        Identifier = channel.Identifier,
        OnlineCount = channel.Online
    };
}
