using System.Collections.Concurrent;

namespace OpenWorker.Navigation;

/// <summary>
/// The navigation mesh of a map, or the reason there is none.
/// </summary>
public readonly record struct NavMeshLoad(NavMesh? Mesh, string? Error);

/// <summary>
/// Gives the navigation mesh of a map by the name of its batch file. A mesh does not change, so it is read once and the
/// maps that are alive share it. The file is read on the thread pool, never on the thread that asks: that is the world loop.
/// </summary>
public sealed class NavMeshProvider(string folder = "navmesh")
{
    private readonly ConcurrentDictionary<string, Lazy<Task<NavMeshLoad>>> _loaded = new();

    /// <summary>
    /// The mesh of a map, or nothing when the map has none or its file is broken: the error says which. Everyone who asks
    /// for a map while it is being read waits for the same reading.
    /// </summary>
    public Task<NavMeshLoad> GetAsync(string name)
    {
        return _loaded.GetOrAdd(name, e => new Lazy<Task<NavMeshLoad>>(() => Task.Run(() => Load(e)))).Value;
    }

    private NavMeshLoad Load(string name)
    {
        var path = Path.Join(folder, $"{name}.hkt");

        try
        {
            return File.Exists(path) ? new NavMeshLoad(NavMesh.Load(path), null) : new NavMeshLoad(null, $"{path} does not exist");
        }

        catch (Exception e) when (e is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            return new NavMeshLoad(null, $"{path} cannot be read: {e.Message}");
        }
    }
}
