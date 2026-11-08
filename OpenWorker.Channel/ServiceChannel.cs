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

/// <summary>
/// The players of one channel of this server. Everything but <see cref="Online"/> belongs to the world loop.
/// </summary>
public sealed class ServiceChannel(short identifier)
{
    // Touched on the world loop only.
    private readonly Dictionary<Entity, ServerSessionComponent> _members = [];

    // The number of members, written on the loop at every join and leave and read from anywhere.
    private int _online;

    public short Identifier { get; } = identifier;

    /// <summary>
    /// How many players the channel has. Safe to read off the loop, by the services that publish it to Redis.
    /// </summary>
    public short Online => (short)Volatile.Read(ref _online);

    public bool Contains(Entity player) => _members.ContainsKey(player);

    public ChannelWorkload Workload => ChannelUtils.GetWorkload(_members.Count);

    public void ForEach(Action<Entity> action)
    {
        foreach (var entity in _members.Keys)
        {
            action(entity);
        }
    }

    /// <summary>
    /// Sends the message to every member, encoded once.
    /// </summary>
    public void Broadcast(IResponseHotspotMessage message)
    {
        if (_members.Count == 0)
        {
            return;
        }

        using var frame = Frame.Encode(message);

        if (frame is null)
        {
            return;
        }

        foreach (var (_, session) in _members)
        {
            session.Send(frame);
        }
    }

    /// <summary>
    /// Sends the message to every member but the player, encoded once.
    /// </summary>
    public void BroadcastToOthers(Entity player, IResponseHotspotMessage message)
    {
        if (_members.Count == 0)
        {
            return;
        }

        using var frame = Frame.Encode(message);

        if (frame is null)
        {
            return;
        }

        foreach (var (member, session) in _members)
        {
            if (member != player)
            {
                session.Send(frame);
            }
        }
    }

    internal void Join(Entity entity, World world)
    {
        BroadcastJoin(entity, world);

        _members[entity] = world.Get<ServerSessionComponent>(entity);

        Volatile.Write(ref _online, _members.Count);
    }

    internal void Leave(Entity player, World world)
    {
        // A connection can close before its player has joined a channel.
        if (_members.Remove(player))
        {
            Volatile.Write(ref _online, _members.Count);

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
        var people = _members.Keys
            .Where(e => e != player)
            .Select(e => new PersonWorldPair(
                PersonSnapshotMapper.CreatePersonValue(world, e, e),
                PersonSnapshotMapper.CreateWorldValue(world, e)))
            .ToList();

        session.Send(new WorldOtherPersonListResponse { People = people });
    }
}
