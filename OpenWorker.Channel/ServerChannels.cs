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

/// <summary>
/// The channels a player is offered and the change of channel. The list holds the channels of this server only, so a
/// change goes to a channel of this process or to none: a channel the server does not run is not found.
/// </summary>
public sealed class ServerChannels(
    IOptions<InstanceOptions> instance,
    IRedisCollection<ChannelCache> channelCache,
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

    /// <summary>
    /// Moves the player to another channel of this server.
    /// </summary>
    public ChannelSwitchResult Switch(Entity entity, short identifier)
    {
        return channels.TrySwitch(entity, identifier);
    }
}
