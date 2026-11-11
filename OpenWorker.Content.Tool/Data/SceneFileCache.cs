using OpenWorker.Content.Tool.Models;

namespace OpenWorker.Content.Tool.Data;

/// <summary>
/// Keeps merged scene meshes so a second look at the same map does not re-parse Vision files.
/// </summary>
internal sealed class SceneFileCache
{
    public static SceneFileCache Shared { get; } = new();

    private readonly object _gate = new();
    private readonly Dictionary<string, SceneMeshDraw> _files = new(StringComparer.Ordinal);

    public SceneMeshDraw Load(string worldFolder, string scenePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(worldFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenePath);
        var key = Path.GetFullPath(scenePath);

        lock (_gate)
        {
            if (_files.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var mesh = SceneCatalog.Load(worldFolder, key);
            _files[key] = mesh;
            return mesh;
        }
    }
}
