using System.Globalization;
using OpenWorker.Content.Tool.Models;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Content.Tool.Data;

/// <summary>
/// Builds the list of items from <c>tb_Item</c>, with names and icons from <c>tb_item_script</c>.
/// </summary>
internal static class ItemCatalog
{
    public static IReadOnlyList<ItemEntry> Load(string resFolder, string? guiFolder, out string status)
    {
        var cache = ResTableCache.Shared;
        var items = cache.Items(resFolder);
        var scripts = cache.ScriptsById(resFolder);
        var icons = new GuiIconCache(guiFolder);
        var entries = new List<ItemEntry>(items.Count);

        foreach (var item in items)
        {
            ResolveScript(scripts, item.ScriptOf, item.Id, out var name, out var iconPath);

            entries.Add(new ItemEntry
            {
                ItemId = item.Id,
                Name = name,
                IconPath = iconPath,
                Classify = item.Classify,
                Rank = item.Rank,
                Level = item.Level,
                LevelLimit = item.LevelLimit,
                Stack = item.Stack,
                SellPrice = item.SellPrice,
                Price = item.Price,
                BindType = item.BindType,
                Model = item.Model,
                SubModel = item.SubModel,
                ScriptItem = item.ScriptItem,
                SubType = item.SubType,
                SlotDisable = item.SlotDisable,
                Endurance = item.Endurance,
                PhysicalAttack = item.PhysicalAttack,
                MagicalAttack = item.MagicalAttack,
                PhysicalDefense = item.PhysicalDefense,
                MagicalDefense = item.MagicalDefense,
                Option = item.Option,
                Socket = item.Socket,
                SetItem = item.SetItem,
                Reinforce = item.Reinforce,
                Evolution = item.Evolution,
                Disassemble = item.Disassemble,
                Furniture = item.Furniture,
                CooldownGroup = item.CooldownGroup,
                CooldownMilliseconds = item.CooldownMilliseconds,
                EffectType = item.EffectType,
                EffectValue = item.EffectValue,
                Cash = item.Cash,
                UsePeriodType = item.UsePeriodType,
                UsePeriodValue = item.UsePeriodValue,
                OtherLookItem = item.OtherLookItem,
                SealLimit = item.SealLimit,
                RestoreLimit = item.RestoreLimit,
                Properties = BuildProperties(item),
                UnknownFields = UnknownFields.Of(item),
                Icon = icons.Get(iconPath)
            });
        }

        status = string.Format(
            CultureInfo.InvariantCulture,
            "Items: {0}{1}",
            entries.Count,
            icons.Missing == 0
                ? string.Empty
                : string.Format(CultureInfo.InvariantCulture, ", {0} icons missing", icons.Missing));

        return entries;
    }

    private static void ResolveScript(
        RowIndex<int, ItemScriptRow> scripts,
        int scriptItem,
        int itemId,
        out string name,
        out string iconPath)
    {
        iconPath = string.Empty;
        name = string.Format(CultureInfo.InvariantCulture, "Item {0}", itemId);

        if (!scripts.TryGet(scriptItem, out var script) &&
            (scriptItem == itemId || !scripts.TryGet(itemId, out script)))
        {
            return;
        }

        iconPath = script.Icon;
        if (!string.IsNullOrWhiteSpace(script.Name))
        {
            name = script.Name;
        }
    }

    private static IReadOnlyList<ItemPropertyRow> BuildProperties(ItemRow item)
    {
        return
        [
            Prop("Classify", item.Classify),
            Prop("Rank", item.Rank),
            Prop("Level", item.Level),
            Prop("Level limit", item.LevelLimit),
            Prop("Class limit", item.ClassLimit),
            Prop("Sell limit", item.SellLimit),
            Prop("Stack", item.Stack),
            Prop("Sell price", item.SellPrice),
            Prop("Buy price", item.Price),
            Prop("Bind", item.BindType),
            Prop("Model", item.Model),
            Prop("Sub-model", item.SubModel),
            Prop("Script item", item.ScriptItem),
            Prop("Sub type", item.SubType),
            Prop("Costume set", item.CostumeSet),
            Prop("Costume set id", item.CostumeSetId),
            Prop("Slot disable", item.SlotDisable),
            Prop("Endurance", item.Endurance),
            Prop("Use value", item.UseValue),
            Prop("Physical attack", item.PhysicalAttack),
            Prop("Magical attack", item.MagicalAttack),
            Prop("Physical defence", item.PhysicalDefense),
            Prop("Magical defence", item.MagicalDefense),
            Prop("Option", item.Option),
            Prop("Socket", item.Socket),
            Prop("Set", item.SetItem),
            Prop("Reinforce", item.Reinforce),
            Prop("Evolution", item.Evolution),
            Prop("Disassemble", item.Disassemble),
            Prop("Furniture", item.Furniture),
            Prop("Cooldown group", item.CooldownGroup),
            Prop("Cooldown ms", item.CooldownMilliseconds),
            Prop("Cooldown saved", item.CooldownSaved),
            Prop("Effect type", item.EffectType),
            Prop("Effect value", item.EffectValue),
            Prop("Cash", item.Cash),
            Prop("Use period type", item.UsePeriodType),
            Prop("Use period value", item.UsePeriodValue),
            Prop("Other look item", item.OtherLookItem),
            Prop("Tucked look", item.TuckedLook),
            Prop("Use effect", item.UseEffect),
            Prop("Seal limit", item.SealLimit),
            Prop("Restore limit", item.RestoreLimit)
        ];
    }

    private static ItemPropertyRow Prop(string name, IFormattable value) =>
        new() { Name = name, Value = value.ToString(null, CultureInfo.InvariantCulture) };

    private static ItemPropertyRow Prop(string name, string value) =>
        new() { Name = name, Value = value };
}
