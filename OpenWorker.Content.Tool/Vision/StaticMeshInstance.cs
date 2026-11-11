using System.Numerics;

namespace OpenWorker.Content.Tool.Vision;

/// <summary>
/// One placed static mesh from a Vision zone archive.
/// </summary>
internal sealed class StaticMeshInstance
{
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Vision row-major matrix with translation in the fourth column.
    /// </summary>
    public Matrix4x4 Matrix { get; set; } = Matrix4x4.Identity;

    public uint VisibleMask { get; set; }

    public bool IsCollisionOnly => VisibleMask == 0 && _hasGeometry;

    private bool _hasGeometry;

    public void SetGeometryMasks(IReadOnlyList<uint> masks)
    {
        _hasGeometry = masks.Count > 0;
        uint combined = 0;
        foreach (var mask in masks)
        {
            combined |= mask;
        }

        VisibleMask = combined;
    }
}
