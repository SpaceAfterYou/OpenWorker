using System.Collections.Concurrent;
using Arch.Core;
using Arch.Core.Extensions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Types;
using OpenWorker.Gameplay.Mapping;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Dtos;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Channels.Enums;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Hotspot.Modules.World.Responses;

namespace OpenWorker.Channel;

public readonly record struct ServiceChannel(short Identifier)
{
    // Changed on the world loop, but the online count is also read from background services.
    private ConcurrentDictionary<Entity, ServerSessionComponent> Members { get; } = new();

    public short Online => (short)Members.Count;

    public bool Contains(Entity player) => Members.ContainsKey(player);

    public ChannelWorkload Workload => ChannelUtils.GetWorkload(Members.Count);

    public void ForEach(Action<Entity> action)
    {
        foreach (var entity in Members.Keys)
        {
            action(entity);
        }
    }

    public void Broadcast(IResponseHotspotMessage message)
    {
        foreach (var session in Members.Values)
        {
            session.Send(message);
        }
    }

    public void BroadcastToOthers(Entity player, IResponseHotspotMessage message)
    {
        foreach (var (member, session) in Members)
        {
            if (member != player)
            {
                session.Send(message);
            }
        }
    }

    internal void Join(Entity entity, World world)
    {
        BroadcastJoin(entity, world);

        Members[entity] = world.Get<ServerSessionComponent>(entity);
    }

    internal void Leave(Entity player, World world)
    {
        // A connection can close before its player has joined a channel.
        if (Members.TryRemove(player, out _))
        {
            Broadcast(new WorldOutInfoPcResponse { Actors = [world.Get<ActorComponent>(player)] });
        }
    }

    private void BroadcastJoin(Entity player, World world)
    {
        ActorValue actor = world.Get<ActorComponent>(player);

        Broadcast(new WorldInInfoPcResponse
        {
            Person = PersonSnapshotMapper.CreatePersonValue(world, player, player),
            World = PersonSnapshotMapper.CreateWorldValue(world, player)
        });

        Broadcast(new PersonUpdateOriginStatListResponse { Actor = actor, Stats = [] });
    }

    public void SendOthers(Entity player, World world)
    {
        var session = world.Get<ServerSessionComponent>(player);
        var people = Members.Keys
            .Where(e => e != player)
            .Select(e => new PersonWorldPair(
                PersonSnapshotMapper.CreatePersonValue(world, e, e),
                PersonSnapshotMapper.CreateWorldValue(world, e)))
            .ToList();

        session.Send(new WorldOtherPersonListResponse { People = people });
    }
}
