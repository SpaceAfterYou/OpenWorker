using System.Numerics;
using Arch.Core;
using OpenWorker.Lua;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// The map of the maze the player stands in, for a ride: the boxes it ends at are the common position boxes of the map, and
/// the life of the rider is the one the maze keeps.
/// </summary>
public sealed class MazeTransportMap(World world) : ITransportMap
{
    public bool TryFindStop(Entity player, int box, out Vector3 position, out float rotation)
    {
        position = default;
        rotation = 0f;

        if (!world.IsAlive(player) || !world.TryGet(player, out LuaMaze? maze) || maze is null ||
            maze.Batch.EventBox.CommonPositions.Find(box) is not { } stop)
        {
            return false;
        }

        position = Vector3.Lerp(stop.PosTopLeft, stop.PosBottomRight, 0.5f);
        rotation = stop.Rotation;

        return true;
    }

    public void Protect(Entity player, TimeSpan length)
    {
        if (length > TimeSpan.Zero && world.IsAlive(player) && world.TryGet(player, out LuaMaze? maze) && maze is not null)
        {
            maze.Creatures.Vitals.Protect(player, length);
        }
    }
}
