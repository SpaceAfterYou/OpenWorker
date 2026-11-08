namespace OpenWorker.LoadTest.Bots;

/// <summary>
/// The path a bot walks: a circle around the place it entered at, in <see cref="Points" /> steps. Every move the
/// bot sends is the next point of it, so the position in a move tells which move it is. The server echoes the
/// position unchanged in the broadcast to everybody who sees the bot, and a bot that receives it finds out
/// which move of which bot it is from the position alone. That is how a move is matched to the time it was
/// sent, without a field of the message that the server may one day check.
/// </summary>
internal static class MoveTrack
{
    /// <summary>
    /// The points of the circle. A bot that waits for the broadcast of a move longer than it takes to send this
    /// many moves would mistake it for a later one, but a delay like that is a failure of the run anyway.
    /// </summary>
    public const int Points = 1024;

    /// <summary>
    /// The radius in game units. Neighbouring points are 0.25 units apart, far more than a float differs by at the
    /// coordinates of a district (about 0.001 near 10000), so a point is told from its neighbour with a margin.
    /// </summary>
    public const float Radius = 40f;

    /// <summary>
    /// How far from the circle a position may be and still be taken for a point of it.
    /// </summary>
    private const float Tolerance = 1f;

    public static (float X, float Y) PointAt(float centerX, float centerY, int index)
    {
        var angle = 2d * Math.PI * (index % Points) / Points;

        return ((float)(centerX + (Radius * Math.Cos(angle))), (float)(centerY + (Radius * Math.Sin(angle))));
    }

    /// <summary>
    /// The heading, in degrees from 0 to 360, of a bot that runs along the circle at the given point.
    /// </summary>
    public static float HeadingAt(int index)
    {
        var degrees = 360d * (index % Points) / Points;

        return (float)((degrees + 90d) % 360d);
    }

    /// <summary>
    /// The index of the point a position is, or -1 when it is not on the circle around the center.
    /// </summary>
    public static int IndexOf(float centerX, float centerY, float x, float y)
    {
        var dx = (double)x - centerX;
        var dy = (double)y - centerY;

        if (Math.Abs(Math.Sqrt((dx * dx) + (dy * dy)) - Radius) > Tolerance)
        {
            return -1;
        }

        var turns = Math.Atan2(dy, dx) / (2d * Math.PI);
        var index = (int)Math.Round(turns * Points);

        return ((index % Points) + Points) % Points;
    }
}
