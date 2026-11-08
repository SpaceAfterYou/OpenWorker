using Arch.Core;
using Microsoft.Extensions.Options;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Gameplay.Modules.Channels.Components;
using OpenWorker.Hotspot.Modules.Channels.Enums;

namespace OpenWorker.Channel;

public sealed class ServiceChannels(IOptions<ChannelOptions> channels, IOptions<InstanceOptions> instance, World world) : List<ServiceChannel>(CreateCollection(channels.Value)), ISessionCloseHandler
{
    private Guid Owner { get; } = instance.Value.Identifier;

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

        // Switch
        
        Leave(entity);
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
        channel.Leave(player, world);
    }

    private static ServiceChannel[] CreateCollection(ChannelOptions options)
    {
        var count = options.To - options.From + 1;

        return Enumerable.Range(options.From, count)
            .Select(e => new ServiceChannel((short)e))
            .ToArray();
    }
}