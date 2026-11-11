namespace OpenWorker.Content.Tool.Models;

/// <summary>
/// One textured triangle list for the mesh viewport: interleaved position (xyz) and UV.
/// </summary>
public sealed class SceneTexturedBatch
{
    /// <summary>
    /// Absolute path to a diffuse <c>.dds</c>, or null to draw with vertex color only.
    /// </summary>
    public required string? TexturePath { get; init; }

    /// <summary>
    /// Interleaved <c>x,y,z,u,v</c> floats in OpenGL Y-up space.
    /// </summary>
    public required float[] Vertices { get; init; }

    public required int TriangleCount { get; init; }
}
