using Arch.Core;
using OpenWorker.Channel;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// Every player has a maze of their own for now, so the only one to tell is the player.
/// </summary>
public sealed class MovementService(World world) : MovementServiceBase(world)
{
    private readonly World _world = world;

    protected override void Broadcast(Entity player, IResponseHotspotMessage message)
    {
        _world.Get<ServerSessionComponent>(player).Send(message);
    }

    protected override void BroadcastToOthers(Entity player, IResponseHotspotMessage message)
    {
    }
}
