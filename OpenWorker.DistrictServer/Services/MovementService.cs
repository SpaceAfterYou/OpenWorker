using System.Numerics;
using Arch.Core;
using OpenWorker.Channel;
using OpenWorker.Hotspot.Modules.Movement.Responses;

namespace OpenWorker.DistrictServer.Services;

/// <summary>
/// A move is heard by the players of the channel who see the mover, once per pass of the world loop, and moves the mover
/// across the visibility grid.
/// </summary>
public sealed class MovementService(World world, ServiceChannels channels) : MovementServiceBase(world)
{
    private readonly World _world = world;

    protected override void Broadcast<T>(Entity player, in T message)
    {
        channels.Get(player).BroadcastNearby(player, in message);
    }

    protected override void BroadcastToOthers<T>(Entity player, in T message)
    {
        channels.Get(player).BroadcastNearbyToOthers(player, in message);
    }

    protected override void Move(Entity player, in MovementMoveBtResponse message)
    {
        channels.Get(player).Move(player, in message);
    }

    protected override void OnPlaced(Entity player, Vector3 position)
    {
        channels.Get(player).Relocate(player, position, _world);
    }
}
