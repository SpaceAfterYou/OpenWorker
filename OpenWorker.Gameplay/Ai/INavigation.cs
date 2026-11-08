using System.Numerics;

namespace OpenWorker.Gameplay.Ai;

/// <summary>
/// The ground of a map as a creature that walks sees it. Without a map of the ground a creature walks wherever it likes.
/// </summary>
public interface INavigation
{
    /// <summary>
    /// The height of the floor under a place, or nothing when there is no floor there: a wall, a pit, the outside.
    /// </summary>
    bool TryGetFloor(Vector3 position, out float height);

    /// <summary>
    /// The way for a creature of a radius from a place to another one: the place it starts at, a point for every turn and
    /// the place it ends at. Nothing when it cannot get there at all: a closed gate, a wall or the other side of a pit.
    /// </summary>
    bool TryFindPath(Vector3 from, Vector3 to, float radius, List<Vector3> path);

    /// <summary>
    /// Whether something at one place can notice something at another: nothing that stands behind a wall is noticed.
    /// </summary>
    bool CanSee(Vector3 from, Vector3 to);
}

/// <summary>
/// The ground has no walls and is as high as the creature is.
/// </summary>
public sealed class OpenNavigation : INavigation
{
    public static OpenNavigation Instance { get; } = new();

    public bool TryGetFloor(Vector3 position, out float height)
    {
        height = position.Z;

        return true;
    }

    public bool TryFindPath(Vector3 from, Vector3 to, float radius, List<Vector3> path)
    {
        path.Clear();
        path.Add(from);
        path.Add(to);

        return true;
    }

    public bool CanSee(Vector3 from, Vector3 to) => true;
}
