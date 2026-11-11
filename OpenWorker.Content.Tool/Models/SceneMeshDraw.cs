using System.Numerics;

namespace OpenWorker.Content.Tool.Models;

/// <summary>
/// Merged Vision static-mesh triangles for a map, already in OpenGL Y-up space
/// (interleaved position + RGBA, same layout as the batch viewport navmesh faces).
/// </summary>
public sealed class SceneMeshDraw
{
    public required float[] Faces { get; init; }

    public required Vector3 Minimum { get; init; }

    public required Vector3 Maximum { get; init; }

    public required int TriangleCount { get; init; }

    public required int InstanceCount { get; init; }

    public required int MeshCount { get; init; }
}
