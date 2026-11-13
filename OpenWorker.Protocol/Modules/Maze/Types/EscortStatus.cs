namespace OpenWorker.Hotspot.Modules.Maze.Types;

/// <summary>
/// What became of the escort at a point.
/// </summary>
public enum EscortStatus : byte
{
    /// <summary>
    /// A point on the way is passed: its light goes out and the next one lights up.
    /// </summary>
    Passed = 1,

    /// <summary>
    /// The last point is reached.
    /// </summary>
    Arrived = 2,

    /// <summary>
    /// The way starts at the point: it lights up.
    /// </summary>
    Started = 3,

    /// <summary>
    /// The escorted has fallen: the light of the point goes out.
    /// </summary>
    Ended = 4
}
