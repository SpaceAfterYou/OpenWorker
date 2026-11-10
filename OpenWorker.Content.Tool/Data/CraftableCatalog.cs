using System.Globalization;
using System.Text;
using OpenWorker.Content.Tool.Models;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Content.Tool.Data;

/// <summary>
/// Builds the list of items a player can make from <c>tb_Make</c>, with names and icons from <c>tb_item_script</c>.
/// </summary>
internal static class CraftableCatalog
{
    public static IReadOnlyList<CraftableItemEntry> Load(string resFolder, string? guiFolder, out string status)
    {
        var cache = ResTableCache.Shared;
        var makes = cache.Makes(resFolder);
        var scripts = cache.ScriptsById(resFolder);
        var items = cache.ItemsById(resFolder);
        var icons = new GuiIconCache(guiFolder);

        var products = makes
            .Where(row => row.Product != 0)
            .GroupBy(row => row.Product)
            .OrderBy(group => group.Key)
            .ToArray();

        var entries = new List<CraftableItemEntry>(products.Length);

        foreach (var group in products)
        {
            ResolveItem(scripts, items, group.Key, out var name, out var iconPath);
            var icon = icons.Get(iconPath);

            var recipes = new List<CraftableRecipe>(group.Count());
            foreach (var make in group.OrderBy(row => row.Id))
            {
                var materials = new List<CraftableMaterialRef>(4);
                var requirements = new List<CraftableRequirementRow>(6);
                foreach (var (item, count) in make.Materials)
                {
                    if (item == 0 || count <= 0)
                    {
                        continue;
                    }

                    ResolveItem(scripts, items, item, out var materialName, out var materialIconPath);
                    var materialIcon = icons.Get(materialIconPath);
                    materials.Add(new CraftableMaterialRef
                    {
                        ItemId = item,
                        Name = materialName,
                        Count = count,
                        Icon = materialIcon
                    });
                    requirements.Add(new CraftableRequirementRow
                    {
                        Kind = "Material",
                        ItemId = item,
                        Name = materialName,
                        Count = count,
                        Icon = materialIcon
                    });
                }

                if (make.Gold > 0)
                {
                    requirements.Add(new CraftableRequirementRow
                    {
                        Kind = "Gold",
                        ItemId = 0,
                        Name = "Gold",
                        Count = make.Gold,
                        Icon = icons.Get("Icon/Item/Goods/Goods_Gold")
                    });
                }

                if (make.BattlePoint > 0)
                {
                    requirements.Add(new CraftableRequirementRow
                    {
                        Kind = "Battle point",
                        ItemId = 0,
                        Name = "BP",
                        Count = make.BattlePoint,
                        Icon = icons.Get("Icon/Item/Goods/Goods_BP")
                    });
                }

                recipes.Add(new CraftableRecipe
                {
                    RecipeId = make.Id,
                    Group = make.Group,
                    Category = make.Category,
                    SubCategory = make.SubCategory,
                    Priority = make.Priority,
                    Gold = make.Gold,
                    BattlePoint = make.BattlePoint,
                    Materials = materials,
                    Requirements = requirements,
                    PathText = FormatPath(materials, make.Gold, make.BattlePoint, name),
                    UnknownFields = UnknownFields.Of(make)
                });
            }

            var unknown = items.TryGet(group.Key, out var product)
                ? UnknownFields.Of(product)
                : [];

            entries.Add(new CraftableItemEntry
            {
                ItemId = group.Key,
                Name = name,
                IconPath = iconPath,
                Recipes = recipes,
                UnknownFields = unknown,
                Icon = icon
            });
        }

        status = string.Format(
            CultureInfo.InvariantCulture,
            "Craftables: {0} products from {1} recipes{2}",
            entries.Count,
            makes.Count,
            icons.Missing == 0
                ? string.Empty
                : string.Format(CultureInfo.InvariantCulture, ", {0} icons missing", icons.Missing));

        return entries;
    }

    private static void ResolveItem(
        RowIndex<int, ItemScriptRow> scripts,
        RowIndex<int, ItemRow> items,
        int itemId,
        out string name,
        out string iconPath)
    {
        iconPath = string.Empty;
        name = string.Format(CultureInfo.InvariantCulture, "Item {0}", itemId);

        var scriptId = items.TryGet(itemId, out var item) ? item.ScriptOf : itemId;
        if (!scripts.TryGet(scriptId, out var script) &&
            (scriptId == itemId || !scripts.TryGet(itemId, out script)))
        {
            return;
        }

        iconPath = script.Icon;
        if (!string.IsNullOrWhiteSpace(script.Name))
        {
            name = script.Name;
        }
    }

    private static string FormatPath(
        List<CraftableMaterialRef> materials,
        int gold,
        short battlePoint,
        string productName)
    {
        var builder = new StringBuilder();

        for (var i = 0; i < materials.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(" + ");
            }

            builder.Append(materials[i].Label);
        }

        if (gold > 0)
        {
            if (builder.Length > 0)
            {
                builder.Append(" + ");
            }

            builder.Append(gold.ToString(CultureInfo.InvariantCulture));
            builder.Append(" gold");
        }

        if (battlePoint > 0)
        {
            if (builder.Length > 0)
            {
                builder.Append(" + ");
            }

            builder.Append(battlePoint.ToString(CultureInfo.InvariantCulture));
            builder.Append(" BP");
        }

        if (builder.Length == 0)
        {
            builder.Append("(no materials)");
        }

        builder.Append(" → ");
        builder.Append(productName);
        return builder.ToString();
    }
}
