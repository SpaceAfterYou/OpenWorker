using System.Numerics;

namespace OpenWorker.Content.Tool.Models;

/// <summary>
/// Vision static meshes for a map, split by <c>.vzone</c> layer, already in OpenGL Y-up space.
/// </summary>
public sealed class SceneMeshDraw
{
    public required IReadOnlyList<SceneZoneLayer> Zones { get; init; }

    /// <summary>
    /// Merged translucent faces of every zone (position + RGBA), for viewports that do not
    /// filter layers themselves.
    /// </summary>
    public required float[] Faces { get; init; }

    public required Vector3 Minimum { get; init; }

    public required Vector3 Maximum { get; init; }

    public required int TriangleCount { get; init; }

    public required int InstanceCount { get; init; }

    public required int MeshCount { get; init; }
}
