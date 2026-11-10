using System.Numerics;
using Arch.Core;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot;

namespace OpenWorker.Gameplay.Modules.Maze;

/// <summary>
/// The portals of the map a player stands in, read from the file of the map: a portal is a box of the map, and a player is at
/// it when it stands within <see cref="Reach"/> of the middle of the box in every direction.
/// </summary>
public sealed class BatchMazePortals(World world, BatchManager batches) : IMazePortals
{
    /// <summary>
    /// How far from the middle of a portal a player may stand and still take it.
    /// </summary>
    public const float Reach = 2000f;

    public bool Exists(Entity player, int portal)
    {
        return Find(player, portal, out _);
    }

    public bool IsAt(Entity player, int portal)
    {
        if (!Find(player, portal, out var middle))
        {
            return false;
        }

        var position = world.Get<WorldComponent>(player).Position;

        return MathF.Abs(position.X - middle.X) <= Reach && MathF.Abs(position.Y - middle.Y) <= Reach && MathF.Abs(position.Z - middle.Z) <= Reach;
    }

    private bool Find(Entity player, int portal, out Vector3 middle)
    {
        middle = default;

        if (!world.IsAlive(player) || !world.TryGet<WorldComponent>(player, out var place) ||
            !batches.TryGetAndCache(place.Location, out var batch) || batch.EventBox.Portals.Find(portal) is not { } box)
        {
            return false;
        }

        middle = (box.PosTopLeft + box.PosBottomRight) / 2;

        return true;
    }
}
