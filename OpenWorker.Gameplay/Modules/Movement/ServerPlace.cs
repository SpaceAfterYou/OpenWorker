using System.Numerics;
using Arch.Core;
using OpenWorker.Gameplay.Messages.Response.Person;

namespace OpenWorker.Gameplay.Modules.Movement;

/// <summary>
/// The server moves a player within its map (a portal, a warp of the maze, the end of a grab or of a ride). The pace check
/// measures the next move from the new place, and drops without a fault the moves the client sent from the old one before
/// it took the new.
/// </summary>
public static class ServerPlace
{
    /// <param name="rotation">The way the player faces there; with none, the way it faced.</param>
    public static void Put(World world, Entity player, Vector3 position, float? rotation = null)
    {
        var place = world.Get<WorldComponent>(player);

        world.Set(player, place with { Position = position, Rotation = rotation ?? place.Rotation });

        if (world.Has<PlaceToldComponent>(player))
        {
            world.Set(player, new PlaceToldComponent(true));
        }
        else
        {
            world.Add(player, new PlaceToldComponent(true));
        }
    }
}
