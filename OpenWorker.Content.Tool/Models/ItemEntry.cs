using System.Globalization;
using Avalonia.Media.Imaging;

namespace OpenWorker.Content.Tool.Models;

/// <summary>
/// One row of <c>tb_Item</c> for the items list, with name and icon from <c>tb_item_script</c>.
/// </summary>
public sealed class ItemEntry
{
    public required int ItemId { get; init; }

    public required string Name { get; init; }

    public required string IconPath { get; init; }

    public required int Classify { get; init; }

    public required byte Rank { get; init; }

    public required short Level { get; init; }

    public required short LevelLimit { get; init; }

    public required short Stack { get; init; }

    public required int SellPrice { get; init; }

    public required int Price { get; init; }

    public required byte BindType { get; init; }

    public required int Model { get; init; }

    public required int SubModel { get; init; }

    public required int ScriptItem { get; init; }

    public required byte SubType { get; init; }

    public required string SlotDisable { get; init; }

    public required byte Endurance { get; init; }

    public required int PhysicalAttack { get; init; }

    public required int MagicalAttack { get; init; }

    public required int PhysicalDefense { get; init; }

    public required int MagicalDefense { get; init; }

    public required int Option { get; init; }

    public required int Socket { get; init; }

    public required int SetItem { get; init; }

    public required short Reinforce { get; init; }

    public required int Evolution { get; init; }

    public required short Disassemble { get; init; }

    public required int Furniture { get; init; }

    public required byte CooldownGroup { get; init; }

    public required int CooldownMilliseconds { get; init; }

    public required short EffectType { get; init; }

    public required int EffectValue { get; init; }

    public required byte Cash { get; init; }

    public required byte UsePeriodType { get; init; }

    public required int UsePeriodValue { get; init; }

    public required int SimilarGroup { get; init; }

    public required byte SealLimit { get; init; }

    public required byte RestoreLimit { get; init; }

    public required IReadOnlyList<ItemPropertyRow> Properties { get; init; }

    /// <summary>
    /// Columns still named <c>FieldN</c> on <c>tb_Item</c>.
    /// </summary>
    public required IReadOnlyList<ItemPropertyRow> UnknownFields { get; init; }

    public bool HasUnknownFields => UnknownFields.Count > 0;

    public Bitmap? Icon { get; init; }

    public string Subtitle =>
        string.Format(
            CultureInfo.InvariantCulture,
            "Id {0} · Rank {1} · Lv {2}",
            ItemId,
            Rank,
            Level);
}
