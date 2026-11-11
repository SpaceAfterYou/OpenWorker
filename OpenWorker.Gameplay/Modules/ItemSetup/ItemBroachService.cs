using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.ItemSetup.Responses;
using OpenWorker.Res;
using OpenWorker.Res.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.ItemSetup;

/// <summary>
/// The broaches of clothing: setting one into an open place, opening a closed place with the item of the clothing's row, and
/// composing five broaches into a new one. A piece of clothing has fifteen places, five groups of three (attack, defence,
/// utility; see <see cref="CostumeBroachPlaces"/>), and keeps the broach of each place; a place without one holds
/// <see cref="Empty"/>, as the client reads it.
/// </summary>
/// <remarks>
/// Runs on the world loop. Every refusal answers the request, so the dialog of the client never waits: setting and opening are
/// refused with an error, a compose with the serial 0, which is how the client hears that nothing was composed. The chances
/// of a compose are in tables only the server has (<see cref="BroachComposeRankRow"/>, <see cref="BroachComposeTypeRow"/>,
/// <see cref="BroachComposeThemeRow"/>, <see cref="BroachComposeItemRow"/>); without them every compose is refused.
/// </remarks>
public sealed class ItemBroachService(
    InventoryService inventory,
    CostumeBroachPlaces places,
    ReadOnlyCollection<BroachComposeRankRow> ranks,
    ReadOnlyCollection<BroachComposeTypeRow> kinds,
    ReadOnlyCollection<BroachComposeThemeRow> themes,
    ReadOnlyCollection<BroachComposeItemRow> composed,
    ItemSetupAnswers answers,
    Random? random = null)
{
    /// <summary>A place without a broach.</summary>
    public const int Empty = -1;

    /// <summary>How many broaches a compose takes.</summary>
    public const int Materials = 5;

    /// <summary>The chances of a compose are out of this.</summary>
    public const int Chances = 10_000;

    // The classify of the first attack broach; the defence and utility ones follow it, then the same three again.
    private const int FirstBroachClassify = 160201;
    private const int BroachClassifies = 6;

    private static readonly int[] Powers = [1, 10, 100, 1000];

    private readonly RowIndex<int, BroachComposeRankRow> _ranks = ranks.IndexBy(e => (int)e.Sum);
    private readonly RowIndex<int, BroachComposeTypeRow> _kinds = kinds.IndexBy(e => (int)e.Sum);
    private readonly RowIndex<int, BroachComposeThemeRow> _themes = themes.IndexBy(e => (int)e.Sum);
    private readonly RowIndex<int, BroachComposeItemRow> _composed = composed.IndexBy(e => e.Index);
    private readonly Random _random = random ?? Random.Shared;

    /// <summary>
    /// Sets a broach into an open place of a piece of clothing that takes its kind. The broach is used up; a broach that was in
    /// the place before is lost with it. A bound broach binds the clothing.
    /// </summary>
    public void Equip(Entity player, StorageSlot targetSlot, StorageSlot broachSlot, byte place)
    {
        const ItemSetupOpcode command = ItemSetupOpcode.BroachEquip;

        if (inventory.InventoryOf(player) is not { } bag || bag.At(targetSlot.Storage, targetSlot.Slot) is not { } target ||
            bag.At(broachSlot.Storage, broachSlot.Slot) is not { Count: > 0 } broach || ReferenceEquals(target, broach) ||
            place >= CostumeBroachPlaces.Places || StateOf(target)[place] != CostumeBroachPlaces.Open ||
            inventory.Catalog.Classify(broach.Item)?.ItemUseType != ItemUseType.ITEM_BROACH ||
            !inventory.Catalog.TryGet(broach.Item, out var row) || KindOf(row.Classify) != place % CostumeBroachPlaces.PlacesPerGroup)
        {
            answers.Refuse(player, command, ItemSetupAnswers.WrongSlot);
            return;
        }

        var broaches = target.Broaches.Length == ST_ITEM_BROACH.ItemCount
            ? target.Broaches.ToArray()
            : Enumerable.Repeat(Empty, ST_ITEM_BROACH.ItemCount).ToArray();

        broaches[place] = broach.Item;
        target.Broaches = broaches;

        var bound = false;

        if (ItemBinding.IsBound(broach.BindState) && ItemBinding.OnBoundBroachSet(target.BindState) is { } bind)
        {
            target.BindState = bind;
            bound = true;
        }

        bag.Touch(target);
        inventory.Spend(player, broach, 1);

        if (bound)
        {
            answers.Send(player, new ItemUpdateResponse { List = [InventoryService.ToStorageValue(target)] });
        }

        answers.Send(player, new ItemSetupBroachEquipResponse { Broach = target.BroachValue()!.Value });
    }

    /// <summary>
    /// Opens a closed place of a piece of clothing for the item and the count of its row.
    /// </summary>
    public void Activate(Entity player, StorageSlot targetSlot, byte place)
    {
        const ItemSetupOpcode command = ItemSetupOpcode.BroachActive;

        if (inventory.InventoryOf(player) is not { } bag || bag.At(targetSlot.Storage, targetSlot.Slot) is not { } target ||
            places.RowOf(target.Item) is not { } row || place >= CostumeBroachPlaces.Places ||
            StateOf(target)[place] != CostumeBroachPlaces.Closed ||
            row.OpenCount <= 0 || !inventory.Catalog.Exists(row.OpenItem) || inventory.CountOf(player, row.OpenItem) < row.OpenCount)
        {
            answers.Refuse(player, command, ItemSetupAnswers.WrongSlot);
            return;
        }

        inventory.TryTake(player, row.OpenItem, row.OpenCount);

        var state = StateOf(target);

        state[place] = CostumeBroachPlaces.Open;
        target.BroachState = new string(state);
        bag.Touch(target);

        answers.Send(player, new ItemSetupBroachActiveResponse { Serial = target.Serial, State = target.BroachState });
    }

    /// <summary>
    /// Composes five broaches into a new one with the item that composes them. The ranks, kinds and themes of the five pick the
    /// rows of the chances; one roll each gives the rank, the kind and the theme of the result, and a last one the broach of
    /// that group. The five broaches and one composing item are used up.
    /// </summary>
    public void Compose(Entity player, StorageSlot toolSlot, IReadOnlyList<StorageSlot> materials)
    {
        if (Pick(player, toolSlot, materials) is not { } pick || !inventory.CanGive(player, pick.Result, 1))
        {
            answers.Send(player, new ItemSetupBroachComposeResponse { Serial = 0 });
            return;
        }

        foreach (var (item, count) in pick.Materials)
        {
            inventory.Spend(player, item, count);
        }

        inventory.Spend(player, pick.Tool, 1);
        inventory.TryGive(player, pick.Result, 1, out var changes);

        var made = changes.Count > 0 ? inventory.InventoryOf(player)?.At(changes[^1].Storage, changes[^1].Slot) : null;

        answers.Send(player, new ItemSetupBroachComposeResponse { Serial = made?.Serial ?? 0 });
    }

    private sealed record ComposePick(ItemInstance Tool, List<(ItemInstance Item, int Count)> Materials, int Result);

    /// <summary>
    /// What a compose takes and gives, or <c>null</c> when it cannot be done. Nothing changes here but the rolls.
    /// </summary>
    private ComposePick? Pick(Entity player, StorageSlot toolSlot, IReadOnlyList<StorageSlot> materials)
    {
        if (materials is not { Count: Materials } || inventory.InventoryOf(player) is not { } bag ||
            bag.At(toolSlot.Storage, toolSlot.Slot) is not { Count: > 0 } tool ||
            inventory.Catalog.Classify(tool.Item)?.ItemUseType != ItemUseType.BROACH_COMPOSE)
        {
            return null;
        }

        var taken = new List<(ItemInstance Item, int Count)>();
        int rankSum = 0, kindSum = 0, themeSum = 0;

        // The same broach may be named more than once when its stack holds enough of it.
        foreach (var slot in materials.GroupBy(e => (e.Storage, e.Slot)))
        {
            if (slot.Key.Storage is not (StorageGroup.Common or StorageGroup.Cash) ||
                bag.At(slot.Key.Storage, slot.Key.Slot) is not { } item || item.Count < slot.Count() ||
                ItemBinding.IsBound(item.BindState) ||
                inventory.Catalog.Classify(item.Item)?.ItemUseType != ItemUseType.ITEM_BROACH ||
                !inventory.Catalog.TryGet(item.Item, out var row) ||
                row.Rank is < 2 or > 6 || KindOf(row.Classify) is not { } kind || row.EffectValue is < 1 or > 4)
            {
                return null;
            }

            // Each broach counts 2, 4, 6, 8 or 10 for its rank, 1, 10 or 100 for its kind and 1, 10, 100 or 1000 for its theme.
            rankSum += slot.Count() * 2 * (row.Rank - 1);
            kindSum += slot.Count() * Powers[kind];
            themeSum += slot.Count() * Powers[row.EffectValue - 1];

            taken.Add((item, slot.Count()));
        }

        if (!_ranks.TryGet(rankSum, out var rankRow) || !_kinds.TryGet(kindSum, out var kindRow) ||
            !_themes.TryGet(themeSum, out var themeRow))
        {
            return null;
        }

        var rank = Roll(rankRow.Chances);
        var kindOf = Roll(kindRow.Chances);
        var theme = Roll(themeRow.Chances);
        var group = 100_000 * theme + 10_000 * kindOf + 1_000 * rank;

        if (!_composed.TryGet(group + 1, out var first) || first.GroupSize == 0)
        {
            return null;
        }

        var number = _random.Next(1, first.GroupSize + 1);

        return _composed.TryGet(group + number, out var result) && inventory.Catalog.Exists(result.Item)
            ? new ComposePick(tool, taken, result.Item)
            : null;
    }

    /// <summary>
    /// Which of the chances a roll from 1 to <see cref="Chances"/> falls in, from 1; 0 when it falls past them all.
    /// </summary>
    private int Roll(ushort[] chances)
    {
        var roll = _random.Next(1, Chances + 1);

        for (var i = 0; i < chances.Length; i++)
        {
            if (roll <= chances[i])
            {
                return i + 1;
            }

            roll -= chances[i];
        }

        return 0;
    }

    /// <summary>
    /// The kind of a broach by its classify: 0 attack, 1 defence, 2 utility, the place in a group it goes to.
    /// </summary>
    private static int? KindOf(int classify)
    {
        var at = classify - FirstBroachClassify;

        return at is >= 0 and < BroachClassifies ? at % CostumeBroachPlaces.PlacesPerGroup : null;
    }

    /// <summary>
    /// The places of a piece of clothing, always <see cref="CostumeBroachPlaces.Places"/> of them.
    /// </summary>
    private static char[] StateOf(ItemInstance item)
    {
        return item.BroachState.PadRight(CostumeBroachPlaces.Places, CostumeBroachPlaces.None)[..CostumeBroachPlaces.Places].ToCharArray();
    }
}
