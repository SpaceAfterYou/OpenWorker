using System.Numerics;
using Arch.Core;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Movement.Responses;
using OpenWorker.Server.Common;
using OpenWorker.Wire;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// Every player has a maze of their own for now, so the only one to hear a player is the player, and there are no others to
/// tell.
/// </summary>
public sealed class MazeAudience(World world) : IPlayerAudience
{
    public void Chat(Entity player, IResponseHotspotMessage message)
    {
        world.Get<ServerSessionComponent>(player).Send(message);
    }

    public void Nearby<T>(Entity player, in T message) where T : struct, IResponseHotspotMessage, IWireWritable
    {
        using var frame = Frame.Encode(in message);

        if (frame is not null)
        {
            world.Get<ServerSessionComponent>(player).Send(frame);
        }
    }

    public void NearbyToOthers<T>(Entity player, in T message) where T : struct, IResponseHotspotMessage, IWireWritable
    {
    }

    public void NearbyToOthers(Entity player, IResponseHotspotMessage message)
    {
    }

    /// <summary>
    /// Nobody else can see the move, so it goes at once.
    /// </summary>
    public void Move(Entity player, in MovementMoveBtResponse message)
    {
        Nearby(player, in message);
    }

    public void Relocate(Entity player, Vector3 position)
    {
    }
}
