using Arch.Core;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Items;

public sealed partial class ItemUseService
{
    // The kind of effect in the item row that names a booster.
    private const int BoosterEffect = 3;

    /// <summary>
    /// A booster item gives its booster, which the item row names; the item is used up when the person gets it.
    /// </summary>
    private async ValueTask<ItemUseResult> UseBoosterAsync(Entity player, Inventory bag, ItemInstance item, ItemRow row, CancellationToken cancellationToken)
    {
        if (boosters is null || row.EffectType != BoosterEffect || !await boosters.AddAsync(player, (short)row.EffectValue, null, cancellationToken))
        {
            return ItemUseResult.Refused();
        }

        // The booster took a while to give: the item must still be the one that was used.
        if (!world.IsAlive(player) || bag.At(item.Storage, item.Slot) != item)
        {
            return ItemUseResult.Refused();
        }

        inventory.Spend(player, item, 1);

        return ItemUseResult.Used();
    }
}
