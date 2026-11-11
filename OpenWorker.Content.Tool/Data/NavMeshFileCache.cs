using OpenWorker.Navigation;

namespace OpenWorker.Content.Tool.Data;

/// <summary>
/// Keeps parsed <c>*.hkt</c> navigation meshes so a second look at the same map does not read the tagfile again.
/// </summary>
internal sealed class NavMeshFileCache
{
    public static NavMeshFileCache Shared { get; } = new();

    private readonly object _gate = new();
    private readonly Dictionary<string, NavMesh> _files = new(StringComparer.Ordinal);

    public NavMesh Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var key = Path.GetFullPath(path);

        lock (_gate)
        {
            if (_files.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var mesh = NavMesh.Load(key);
            _files[key] = mesh;
            return mesh;
        }
    }
}
