using System.Numerics;

namespace OpenWorker.Gameplay.Ai;

/// <summary>
/// Where a creature can walk. Without a map of the ground a creature walks wherever it likes.
/// </summary>
public interface INavigation
{
    /// <summary>
    /// The place a creature that sets off from one point towards another can reach, or nothing when it cannot walk there at all.
    /// </summary>
    Vector3? Clamp(Vector3 from, Vector3 target);

    /// <summary>
    /// Whether something at one place can notice something at another: nothing that stands behind a wall is noticed.
    /// </summary>
    bool CanSee(Vector3 from, Vector3 target);
}

/// <summary>
/// The ground has no walls.
/// </summary>
public sealed class OpenNavigation : INavigation
{
    public static OpenNavigation Instance { get; } = new();

    public Vector3? Clamp(Vector3 from, Vector3 target) => target;

    public bool CanSee(Vector3 from, Vector3 target) => true;
}
