using OpenWorker.Content.Tool.Models;

namespace OpenWorker.Content.Tool.Data;

/// <summary>
/// Keeps parsed scene geometry so a second look at the same map does not re-parse Vision files.
/// Each <see cref="Load"/> returns fresh zone layer objects so visibility toggles stay per view.
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
            if (!_files.TryGetValue(key, out var cached))
            {
                cached = SceneCatalog.Load(worldFolder, key);
                _files[key] = cached;
            }

            return CloneForUi(cached);
        }
    }

    private static SceneMeshDraw CloneForUi(SceneMeshDraw source)
    {
        var zones = new SceneZoneLayer[source.Zones.Count];
        for (var i = 0; i < source.Zones.Count; i++)
        {
            var zone = source.Zones[i];
            zones[i] = new SceneZoneLayer
            {
                Name = zone.Name,
                ColorFaces = zone.ColorFaces,
                TexturedBatches = zone.TexturedBatches,
                TriangleCount = zone.TriangleCount,
                InstanceCount = zone.InstanceCount,
                IsVisible = true
            };
        }

        return new SceneMeshDraw
        {
            Zones = zones,
            Faces = source.Faces,
            Minimum = source.Minimum,
            Maximum = source.Maximum,
            TriangleCount = source.TriangleCount,
            InstanceCount = source.InstanceCount,
            MeshCount = source.MeshCount
        };
    }
}
