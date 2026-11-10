using Avalonia.Media.Imaging;

namespace OpenWorker.Content.Tool.Models;

/// <summary>
/// One line of what a recipe needs: a material or a currency.
/// </summary>
public sealed class CraftableRequirementRow
{
    public required string Kind { get; init; }

    public required int ItemId { get; init; }

    public required string Name { get; init; }

    public required int Count { get; init; }

    public Bitmap? Icon { get; init; }
}
