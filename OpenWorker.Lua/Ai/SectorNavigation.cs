using System.Numerics;
using OpenWorker.Batch.Entities;
using OpenWorker.Gameplay.Ai;

namespace OpenWorker.Lua.Ai;

/// <summary>
/// The only map of the ground the server has for a maze: the boxes of its sectors. A sector is a room or a piece of a corridor,
/// and what leads from one to another is a gate, so a creature that was born in a sector does not leave its box and
/// does not notice what is outside. Where no box covers a place there is no limit.
/// </summary>
public sealed class SectorNavigation : INavigation
{
    /// <summary>
    /// How far from a wall of the box a creature keeps.
    /// </summary>
    private const float Margin = 40f;

    private readonly (Vector2 Minimum, Vector2 Maximum)[] _boxes;

    public SectorNavigation(IEnumerable<(Vector2 First, Vector2 Second)> boxes)
    {
        _boxes = boxes
            .Select(e => (Vector2.Min(e.First, e.Second), Vector2.Max(e.First, e.Second)))
            .ToArray();
    }

    public SectorNavigation(IEnumerable<SectorBox> sectors)
        : this(sectors.Select(e => (new Vector2(e.PosTopLeft.X, e.PosTopLeft.Y), new Vector2(e.PosBottomRight.X, e.PosBottomRight.Y))))
    {
    }

    public Vector3? Clamp(Vector3 from, Vector3 target)
    {
        if (Find(from) is not { } box)
        {
            return target;
        }

        var middle = (box.Minimum + box.Maximum) / 2f;

        // A box too narrow for the margin leaves the creature in its middle.
        var low = new Vector2(Math.Min(box.Minimum.X + Margin, middle.X), Math.Min(box.Minimum.Y + Margin, middle.Y));
        var high = new Vector2(Math.Max(box.Maximum.X - Margin, middle.X), Math.Max(box.Maximum.Y - Margin, middle.Y));

        return new Vector3(Math.Clamp(target.X, low.X, high.X), Math.Clamp(target.Y, low.Y, high.Y), target.Z);
    }

    public bool CanSee(Vector3 from, Vector3 target)
    {
        return Find(from) is not { } box || Find(target) is null || Contains(box, target);
    }

    private (Vector2 Minimum, Vector2 Maximum)? Find(Vector3 point)
    {
        foreach (var box in _boxes)
        {
            if (Contains(box, point))
            {
                return box;
            }
        }

        return null;
    }

    private static bool Contains((Vector2 Minimum, Vector2 Maximum) box, Vector3 point)
    {
        return point.X >= box.Minimum.X && point.X <= box.Maximum.X && point.Y >= box.Minimum.Y && point.Y <= box.Maximum.Y;
    }
}
