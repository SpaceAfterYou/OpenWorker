using System.Numerics;

namespace OpenWorker.Content.Tool.Models;

/// <summary>
/// A line-and-triangle mesh for an orbit viewport: interleaved position and RGBA, OpenGL Y-up.
/// </summary>
public sealed class OrbitScene
{
    public required float[] Faces { get; init; }

    public required float[] Lines { get; init; }

    public required Vector3 Minimum { get; init; }

    public required Vector3 Maximum { get; init; }
}
