using System.Numerics;
using OpenWorker.Batch;
using OpenWorker.Gameplay.Ai;
using OpenWorker.Navigation;

namespace OpenWorker.Lua.Ai;

/// <summary>
/// The ground of a maze: the navigation mesh of its map, with the gates of the map as boxes nothing walks through while
/// the gate is closed.
/// </summary>
public sealed class NavMeshNavigation : INavigation
{
    private readonly NavField _field;
    private readonly Dictionary<int, int> _gates = [];

    public NavMeshNavigation(NavMesh mesh, VBatchFile batch)
    {
        _field = new NavField(mesh);

        foreach (var gate in batch.EventBox.ServerGates.DistinctBy(e => e.Id))
        {
            var floor = new Vector3(
                (gate.PosTopLeft.X + gate.PosBottomRight.X) / 2f,
                (gate.PosTopLeft.Y + gate.PosBottomRight.Y) / 2f,
                gate.PosTopLeft.Z);

            _gates[gate.Id] = _field.AddBlocker(floor, gate.Size, gate.Rotation, gate.Type != default);
        }
    }

    public NavField Field => _field;

    /// <summary>
    /// A gate has been opened or closed.
    /// </summary>
    public void SetGate(int gate, bool open)
    {
        if (_gates.TryGetValue(gate, out var blocker))
        {
            _field.SetBlocker(blocker, !open);
        }
    }

    public bool TryGetFloor(Vector3 position, out float height)
    {
        return _field.TryGetFloor(position, out height);
    }

    public bool TryFindPath(Vector3 from, Vector3 to, float radius, List<Vector3> path)
    {
        return _field.TryFindPath(from, to, radius, path);
    }

    public bool CanSee(Vector3 from, Vector3 to)
    {
        return _field.CanSee(from, to);
    }
}
