using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenWorker.Content.Tool.Models;

/// <summary>
/// One Vision <c>.vzone</c> as a Photoshop-style layer that can be shown or hidden.
/// </summary>
public sealed partial class SceneZoneLayer : ObservableObject
{
    public required string Name { get; init; }

    /// <summary>
    /// Translucent solid-color triangles (position + RGBA) for Event Box / NavMesh overlays.
    /// </summary>
    public required float[] ColorFaces { get; init; }

    public required IReadOnlyList<SceneTexturedBatch> TexturedBatches { get; init; }

    public required int TriangleCount { get; init; }

    public required int InstanceCount { get; init; }

    [ObservableProperty]
    public partial bool IsVisible { get; set; } = true;

    public string Title => Name;

    public string Subtitle =>
        string.Format(
            CultureInfo.InvariantCulture,
            "{0} tris, {1} instances",
            TriangleCount,
            InstanceCount);
}
