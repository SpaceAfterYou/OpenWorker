namespace OpenWorker.Navigation;

/// <summary>
/// Gives the navigation mesh of a map by the name of its batch file. A mesh does not change, so it is read once and the
/// maps that are alive share it.
/// </summary>
public sealed class NavMeshProvider
{
    private readonly object _lock = new();
    private readonly Dictionary<string, (NavMesh? Mesh, string? Error)> _loaded = [];

    /// <summary>
    /// The mesh of a map, or nothing when the map has none or its file is broken: the error says which.
    /// </summary>
    public NavMesh? Get(string name, out string? error)
    {
        lock (_lock)
        {
            if (!_loaded.TryGetValue(name, out var entry))
            {
                entry = Load(name);

                _loaded.Add(name, entry);
            }

            error = entry.Error;

            return entry.Mesh;
        }
    }

    private static (NavMesh? Mesh, string? Error) Load(string name)
    {
        var path = Path.Join("navmesh", $"{name}.hkt");

        try
        {
            return File.Exists(path) ? (NavMesh.Load(path), null) : (null, $"{path} does not exist");
        }

        catch (Exception e) when (e is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            return (null, $"{path} cannot be read: {e.Message}");
        }
    }
}
