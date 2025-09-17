using Arch.Core;
using Microsoft.Extensions.Configuration;
using OpenWorker.Extensions;
using OpenWorker.Hotspot.Modules.Channels.Components;
using OpenWorker.Hotspot.Modules.Channels.Enums;
using OpenWorker.Hotspot.Modules.Channels.Extensions;

namespace OpenWorker.Channel;

public sealed class ServiceChannels(IConfiguration configuration, World world) : List<ServiceChannel>(CreateCollection(configuration))
{
    private Guid Owner { get; } = configuration.GetInstance();

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

    private void Leave(Entity player)
    {
        var component = world.Get<ChannelMemberComponent>(player);
        
        var channel = this[component.Index];
        channel.Leave(player, world);
    }

    private static ServiceChannel[] CreateCollection(IConfiguration configuration)
    {
        var first = configuration.GetChannelRangeStart();
        var last  = configuration.GetChannelRangeEnd();

        var count = last - first + 1;

        return Enumerable.Range(first, count)
            .Select(e => new ServiceChannel((short)e))
            .ToArray();
    }
}