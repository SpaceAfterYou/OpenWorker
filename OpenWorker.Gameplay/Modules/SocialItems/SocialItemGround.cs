using System.Numerics;

namespace OpenWorker.Gameplay.Modules.SocialItems;

/// <summary>
/// The ground of a district as a social object needs it: the boxes of the map where nothing may be put down, and whether a
/// point has floor under it.
/// </summary>
/// <param name="excluded">The boxes kept free: the centre, the size and the turn in degrees about the vertical.</param>
/// <param name="floor">Whether a point has floor close above or below it; every point has when <see langword="null"/>.</param>
public sealed class SocialItemGround(IEnumerable<(Vector3 Center, Vector3 Size, float Rotation)> excluded, Func<Vector3, bool>? floor = null)
{
    public static SocialItemGround Open { get; } = new([]);

    // Each box as the square it covers once its size is turned with it, as the original measures it.
    private readonly (Vector2 Min, Vector2 Max)[] _boxes = excluded.Select(Cover).ToArray();

    /// <summary>
    /// Whether the point stands inside a box kept free.
    /// </summary>
    public bool IsExcluded(Vector3 point)
    {
        foreach (var (min, max) in _boxes)
        {
            if (point.X > min.X && point.X < max.X && point.Y > min.Y && point.Y < max.Y)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether an object of a radius can stand at the point: there is floor at the four points the radius away along the axes.
    /// </summary>
    public bool HasFloor(Vector3 point, float radius)
    {
        return floor is null ||
               (floor(point with { X = point.X + radius }) &&
                floor(point with { X = point.X - radius }) &&
                floor(point with { Y = point.Y + radius }) &&
                floor(point with { Y = point.Y - radius }));
    }

    private static (Vector2 Min, Vector2 Max) Cover((Vector3 Center, Vector3 Size, float Rotation) box)
    {
        var turned = Vector3.Abs(Vector3.Transform(box.Size, Matrix4x4.CreateRotationZ(box.Rotation * MathF.PI / 180f)));
        var half = new Vector2(turned.X, turned.Y) / 2f;
        var center = new Vector2(box.Center.X, box.Center.Y);

        return (center - half, center + half);
    }
}

/// <summary>
/// Gives the ground of the district this server runs, read once.
/// </summary>
public interface ISocialItemGrounds
{
    ValueTask<SocialItemGround> GetAsync();
}
