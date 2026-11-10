using System.Globalization;
using System.Text;
using Avalonia.Media.Imaging;
using OpenWorker.Content.Tool.Models;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Content.Tool.Data;

/// <summary>
/// Builds the list of items a player can make from <c>tb_Make</c>, with names and icons from <c>tb_item_script</c>.
/// </summary>
internal static class CraftableCatalog
{
    public static IReadOnlyList<CraftableItemEntry> Load(string resFolder, string? guiFolder, out string status)
    {
        var makes = ResTableLoader.Load<MakeRow>(resFolder, "tb_Make");
        var scripts = ResTableLoader
            .Load<ItemScriptRow>(resFolder, "tb_item_script")
            .GroupBy(row => row.Id)
            .ToDictionary(group => group.Key, group => group.First());

        var iconCache = new Dictionary<string, Bitmap?>(StringComparer.Ordinal);
        var products = makes
            .Where(row => row.Product != 0)
            .GroupBy(row => row.Product)
            .OrderBy(group => group.Key)
            .ToArray();

        var missingIcons = 0;
        var entries = new List<CraftableItemEntry>(products.Length);

        foreach (var group in products)
        {
            ResolveItem(scripts, group.Key, out var name, out var iconPath);
            var icon = LoadIcon(guiFolder, iconPath, iconCache, ref missingIcons);

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

                    ResolveItem(scripts, item, out var materialName, out var materialIconPath);
                    var materialIcon = LoadIcon(guiFolder, materialIconPath, iconCache, ref missingIcons);
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
                        Icon = LoadIcon(guiFolder, "Icon/Item/Goods/Goods_Gold", iconCache, ref missingIcons)
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
                        Icon = LoadIcon(guiFolder, "Icon/Item/Goods/Goods_BP", iconCache, ref missingIcons)
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
                    PathText = FormatPath(materials, make.Gold, make.BattlePoint, name)
                });
            }

            entries.Add(new CraftableItemEntry
            {
                ItemId = group.Key,
                Name = name,
                IconPath = iconPath,
                Recipes = recipes,
                Icon = icon
            });
        }

        status = string.Format(
            CultureInfo.InvariantCulture,
            "Craftables: {0} products from {1} recipes{2}",
            entries.Count,
            makes.Count,
            missingIcons == 0
                ? string.Empty
                : string.Format(CultureInfo.InvariantCulture, ", {0} icons missing", missingIcons));

        return entries;
    }

    private static void ResolveItem(
        Dictionary<int, ItemScriptRow> scripts,
        int itemId,
        out string name,
        out string iconPath)
    {
        iconPath = string.Empty;
        name = string.Format(CultureInfo.InvariantCulture, "Item {0}", itemId);

        if (!scripts.TryGetValue(itemId, out var script))
        {
            return;
        }

        iconPath = script.Icon ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(script.Name))
        {
            name = script.Name;
        }
    }

    private static Bitmap? LoadIcon(
        string? guiFolder,
        string iconPath,
        Dictionary<string, Bitmap?> cache,
        ref int missingIcons)
    {
        if (string.IsNullOrWhiteSpace(iconPath))
        {
            return null;
        }

        if (cache.TryGetValue(iconPath, out var cached))
        {
            return cached;
        }

        if (string.IsNullOrWhiteSpace(guiFolder))
        {
            missingIcons++;
            cache[iconPath] = null;
            return null;
        }

        var file = Path.Combine(
            guiFolder,
            iconPath.Replace('/', Path.DirectorySeparatorChar) + ".png");

        if (!File.Exists(file))
        {
            missingIcons++;
            cache[iconPath] = null;
            return null;
        }

        var bitmap = new Bitmap(file);
        cache[iconPath] = bitmap;
        return bitmap;
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
