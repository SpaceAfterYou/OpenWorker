using System.Numerics;
using Arch.Core;
using OpenWorker.Batch.Entities;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot;

namespace OpenWorker.Gameplay.Modules.Maze;

/// <summary>
/// The portals of the map a player stands in, read from the file of the map: a portal is a box of the map, and a player is at
/// it when it stands within <see cref="Reach"/> of the middle of the box in every direction. The box says where it leads: its
/// kind, the location and start point it names, and the maze board it opens when it shows one.
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

    public PortalWay? WayOf(Entity player, int portal)
    {
        if (Box(player, portal) is not { } box)
        {
            return null;
        }

        // A location is a short on the wire; a portal that names a larger one leads nowhere the client can ask for.
        var location = box.JumpMap is >= short.MinValue and <= short.MaxValue ? (short)box.JumpMap : (short)0;

        return new PortalWay(box.JumpType, location, box.Jump, box.ShowGui ? box.Gui : 0);
    }

    private bool Find(Entity player, int portal, out Vector3 middle)
    {
        middle = default;

        if (Box(player, portal) is not { } box)
        {
            return false;
        }

        middle = (box.PosTopLeft + box.PosBottomRight) / 2;

        return true;
    }

    private PortalBox? Box(Entity player, int portal)
    {
        if (!world.IsAlive(player) || !world.TryGet<WorldComponent>(player, out var place) || !batches.TryGetAndCache(place.Location, out var batch))
        {
            return null;
        }

        return batch.EventBox.Portals.Find(portal);
    }
}
