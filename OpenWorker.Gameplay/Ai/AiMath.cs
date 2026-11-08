using System.Numerics;

namespace OpenWorker.Gameplay.Ai;

/// <summary>
/// Directions as the game keeps them: the angle is in degrees and zero looks along -Y.
/// </summary>
public static class AiMath
{
    private const float DegreesToRadians = MathF.PI / 180f;

    public static float Distance2D(Vector3 from, Vector3 to)
    {
        return MathF.Sqrt((to.X - from.X) * (to.X - from.X) + (to.Y - from.Y) * (to.Y - from.Y));
    }

    /// <summary>
    /// The angle of a direction on the ground, between -180 and 180.
    /// </summary>
    public static float YawOf(float x, float y)
    {
        return MathF.Atan2(x, -y) / DegreesToRadians;
    }

    public static float YawTo(Vector3 from, Vector3 to)
    {
        return YawOf(to.X - from.X, to.Y - from.Y);
    }

    /// <summary>
    /// The way a character looks at an angle.
    /// </summary>
    public static Vector2 Direction(float yaw)
    {
        var (sin, cos) = MathF.SinCos(yaw * DegreesToRadians);

        return new Vector2(sin, -cos);
    }

    /// <summary>
    /// How far apart two angles are, between 0 and 180.
    /// </summary>
    public static float AngleBetween(float first, float second)
    {
        var difference = (first - second) % 360f;

        if (difference > 180f)
        {
            difference -= 360f;
        }
        else if (difference < -180f)
        {
            difference += 360f;
        }

        return MathF.Abs(difference);
    }

    /// <summary>
    /// Turns a flat vector by an angle.
    /// </summary>
    public static Vector2 Rotate(Vector2 vector, float degrees)
    {
        var (sin, cos) = MathF.SinCos(degrees * DegreesToRadians);

        return new Vector2(vector.X * cos - vector.Y * sin, vector.X * sin + vector.Y * cos);
    }
}
