using Arch.Core;
using Microsoft.Extensions.Options;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Modules.Channels.Enums;
using OpenWorker.Hotspot.Modules.Channels.Responses;
using OpenWorker.Hotspot.Modules.Channels.Types;
using Redis.OM;
using Redis.OM.Searching;

namespace OpenWorker.Channel;

public sealed class ServerChannels(
    IOptions<InstanceOptions> instance,
    IRedisCollection<ChannelCache> channelCache,
    // IRedisCollection<DistrictCache> districtCache,
    ServiceChannels channels,
    World world
) {
    private Guid Owner { get; } = instance.Value.Identifier;
    private short Location { get; } = instance.Value.Location;

    public async ValueTask SendListAsync(Entity entity, CancellationToken cancellationToken)
    {
        var session = world.Get<ServerSessionComponent>(entity);

        var all = await channelCache
            .Where(e => e.Owner == Owner && e.Location == Location)
            .ToArrayAsync(cancellationToken);
        
        var values = all
            .Select(e => new ChannelValue(e.Identifier, e.Workload))
            .ToArray();

        session.Send(new ChannelInfoResponse
        {
            Location = Location,
            Values = values
        });
    }

    public async ValueTask<ChannelSwitchResult> SwitchAsync(Entity entity, short identifier)
    {
        var result = channels.TrySwitch(entity, identifier);

        if (result is ChannelSwitchResult.Ok)
        {
            return result;
        }
        
        return await MoveAsync(entity, identifier);
    }

    private static ValueTask<ChannelSwitchResult> MoveAsync(Entity entity, short identifier)
    {
        // var desired = channelCache.First(e => e.Identifier == identifier);
        // var district = districtCache.First(e => e.Guid != Owner);

        // TODO: switch the server

        return ValueTask.FromResult(ChannelSwitchResult.Ok);
    }
}