using Arch.Core;
using OpenWorker.Channel;
using OpenWorker.Hotspot;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// Every player has a maze of their own for now, so the only one to tell is the player.
/// </summary>
public sealed class MovementService(World world) : MovementServiceBase(world)
{
    private readonly World _world = world;

    protected override void Broadcast<T>(Entity player, in T message)
    {
        using var frame = Frame.Encode(in message);

        if (frame is not null)
        {
            _world.Get<ServerSessionComponent>(player).Send(frame);
        }
    }

    protected override void BroadcastToOthers<T>(Entity player, in T message)
    {
    }
}
