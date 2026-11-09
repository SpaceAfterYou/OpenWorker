using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Logging;
using OpenWorker.Channel;
using OpenWorker.Hotspot.Handler;
using OpenWorker.Hotspot.Modules.Movement.Responses;

namespace OpenWorker.Server.Common.Services;

/// <summary>
/// A move is heard by the audience of the server: in a district the players of the channel who see the mover, once per pass
/// of the world loop, and the mover moves across the visibility grid; in a maze the mover alone.
/// </summary>
public sealed class MovementService(
    World world,
    IPlayerAudience audience,
    MovementPace pace,
    HandlerExecutor errors,
    ILogger<MovementService> logger
) :
    MovementServiceBase(world, pace, errors, logger)
{
    protected override void Broadcast<T>(Entity player, in T message)
    {
        audience.Nearby(player, in message);
    }

    protected override void BroadcastToOthers<T>(Entity player, in T message)
    {
        audience.NearbyToOthers(player, in message);
    }

    protected override void Move(Entity player, in MovementMoveBtResponse message)
    {
        audience.Move(player, in message);
    }

    protected override void OnPlaced(Entity player, Vector3 position)
    {
        audience.Relocate(player, position);
    }
}
