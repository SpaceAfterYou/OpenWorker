using Arch.Core;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Modules.Channels.Enums;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Hotspot.Modules.World.Responses;

namespace OpenWorker.Channel;

public readonly record struct ServiceChannel(short Identifier)
{
    private HashSet<Entity> InternalMemberCollection { get; } = [];

    public short Online => (short)InternalMemberCollection.Count;

    public ChannelWorkload Workload => ChannelUtils.GetWorkload(InternalMemberCollection.Count);

    public void ForEach(Action<Entity> action)
    {
        foreach (var entity in InternalMemberCollection)
        {
            action(entity);
        }
    }

    internal void Join(Entity entity, World world)
    {
        BroadcastJoin(entity, world);
        
        InternalMemberCollection.Add(entity);
    }

    internal void Leave(Entity player, World world)
    {
        InternalMemberCollection.Remove(player);

        BroadcastLeave(player, world);
    }

    private void BroadcastLeave(Entity player, World world)
    {
        foreach (var session in InternalMemberCollection.Select(member => world.Get<ServerSessionComponent>(member)))
        {
            session.Send(new WorldOutInfoPcResponse(world, player));
        }
    }

    private void BroadcastJoin(Entity player, World world)
    {
        foreach (var session in InternalMemberCollection.Select(member => world.Get<ServerSessionComponent>(member)))
        {
            session.Send(new WorldInInfoPcResponse(world, player));
            session.Send(new PersonUpdateOriginStatListResponse(world, player));
        }
    }

    public void SendOthers(Entity player, World world)
    {
        var session = world.Get<ServerSessionComponent>(player);
        session.Send(new WorldOtherPersonListResponse(InternalMemberCollection));
    }
}