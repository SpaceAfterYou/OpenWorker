using Avalonia.Media.Imaging;

namespace OpenWorker.Content.Tool.Models;

/// <summary>
/// One product of <c>tb_Make</c> for the craftables list.
/// </summary>
public sealed class CraftableItemEntry
{
    public required int ItemId { get; init; }

    public required string Name { get; init; }

    public required string IconPath { get; init; }

    public required IReadOnlyList<CraftableRecipe> Recipes { get; init; }

    /// <summary>
    /// Columns still named <c>FieldN</c> on the product's <c>tb_Item</c> row, if any.
    /// </summary>
    public required UnknownFieldTable UnknownFields { get; init; }

    public bool HasUnknownFields => UnknownFields.HasFields;

    public Bitmap? Icon { get; init; }

    public int RecipeCount => Recipes.Count;

    public string Subtitle =>
        RecipeCount == 1
            ? $"Id {ItemId}"
            : $"Id {ItemId} · {RecipeCount} recipes";

    /// <summary>Craft path of the first recipe, for the list row.</summary>
    public string PrimaryPath => Recipes.Count == 0 ? string.Empty : Recipes[0].PathText;
}
