using Arch.Core;
using Microsoft.Extensions.Options;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Gameplay.Modules.Channels.Components;
using OpenWorker.Hotspot.Modules.Channels.Enums;

namespace OpenWorker.Channel;

/// <summary>
/// The channels of this server. A join or a leave of any of them notifies <see cref="Changes"/>, which the service that
/// publishes the counts to Redis waits for.
/// </summary>
public sealed class ServiceChannels : List<ServiceChannel>, ISessionCloseHandler
{
    private readonly World world;

    /// <param name="sight">The size of the cells of the visibility grid; the default one when <see langword="null"/>.</param>
    public ServiceChannels(IOptions<ChannelOptions> channels, World world, IOptions<WorldOptions>? sight = null)
        : this(channels.Value, sight?.Value ?? new WorldOptions(), new ChannelChanges(), world)
    {
    }

    private ServiceChannels(ChannelOptions channels, WorldOptions sight, ChannelChanges changes, World world)
        : base(CreateCollection(channels, sight, changes))
    {
        this.world = world;
        Changes = changes;
    }

    internal ChannelChanges Changes { get; }

    public ServiceChannel Get(Entity entity)
    {
        var component = world.Get<ChannelMemberComponent>(entity);

        return this[component.Index];
    }
    
    internal ChannelSwitchResult TrySwitch(Entity entity, short identifier)
    {
        var component = world.Get<ChannelMemberComponent>(entity);
        
        // Check the identifier
        
        var current = this[component.Index];

        if (current.Identifier == identifier)
        {
            return ChannelSwitchResult.Same;
        }
        
        // Check the index
        
        var index = (short)FindIndex(e => e.Identifier == identifier);

        if (index == component.Index)
        {
            return ChannelSwitchResult.Same;
        }
        
        if (index == -1)
        {
            return ChannelSwitchResult.NotFound;
        }
        
        // Check the workload
        
        var desired = this[index];
        
        if (desired.Workload == ChannelWorkload.Full)
        {
            return ChannelSwitchResult.Full;
        }

        // Switch: the player stays on the server, so it is told the people of the old channel are gone.
        
        current.Leave(entity, tellPlayer: true);
        Join(index, entity);

        return ChannelSwitchResult.Ok;
    }

    public void Join(short index, Entity entity)
    {
        var channel = this[index];
        channel.Join(entity, world);
        
        world.Set(entity, new ChannelMemberComponent(index));
    }

    public ValueTask OnSessionClosedAsync(Entity player)
    {
        if (Count > 0 && world.Has<ChannelMemberComponent>(player))
        {
            Leave(player);
        }

        return ValueTask.CompletedTask;
    }

    private void Leave(Entity player)
    {
        var component = world.Get<ChannelMemberComponent>(player);
        
        var channel = this[component.Index];
        channel.Leave(player);
    }

    private static ServiceChannel[] CreateCollection(ChannelOptions options, WorldOptions sight, ChannelChanges changes)
    {
        var count = options.To - options.From + 1;

        return Enumerable.Range(options.From, count)
            .Select(e => new ServiceChannel((short)e, sight.VisibilityRadius, changes))
            .ToArray();
    }
}