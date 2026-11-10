using Avalonia.Media.Imaging;

namespace OpenWorker.Content.Tool.Models;

/// <summary>
/// One material of a make recipe.
/// </summary>
public sealed class CraftableMaterialRef
{
    public required int ItemId { get; init; }

    public required string Name { get; init; }

    public required short Count { get; init; }

    public Bitmap? Icon { get; init; }

    public string Label => $"{Name} ×{Count}";
}
