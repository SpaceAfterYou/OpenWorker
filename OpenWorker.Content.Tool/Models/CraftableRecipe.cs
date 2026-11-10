namespace OpenWorker.Content.Tool.Models;

/// <summary>
/// One row of <c>tb_Make</c> that produces a craftable item.
/// </summary>
public sealed class CraftableRecipe
{
    public required int RecipeId { get; init; }

    public required int Group { get; init; }

    public required int Category { get; init; }

    public required int SubCategory { get; init; }

    public required short Priority { get; init; }

    public required int Gold { get; init; }

    public required short BattlePoint { get; init; }

    public required IReadOnlyList<CraftableMaterialRef> Materials { get; init; }

    /// <summary>
    /// Materials and currencies required to craft once, for the requirements table.
    /// </summary>
    public required IReadOnlyList<CraftableRequirementRow> Requirements { get; init; }

    /// <summary>
    /// Human-readable craft path: materials and costs, then the product name.
    /// </summary>
    public required string PathText { get; init; }

    public string MetaText =>
        $"Recipe {RecipeId} · group {Group} · tab {Category}/{SubCategory} · priority {Priority}";
}
