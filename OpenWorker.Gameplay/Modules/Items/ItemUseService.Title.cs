using Arch.Core;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Items;

public sealed partial class ItemUseService
{
    // The kind of effect in the item row that names a title.
    private const int TitleEffect = 4;

    /// <summary>
    /// An item of a title gives the title, unless the person has it or cannot have it.
    /// </summary>
    private async ValueTask<ItemUseResult> UseTitleAsync(Entity player, ItemInstance item, ItemRow row)
    {
        var granter = titles ?? new NoItemTitles();

        if (row.EffectType != TitleEffect || granter.CannotGrant(player, row.EffectValue))
        {
            return ItemUseResult.Refused();
        }

        var bag = inventory.InventoryOf(player);

        if (!await granter.GrantAsync(player, row.EffectValue) || !world.IsAlive(player) || bag?.At(item.Storage, item.Slot) != item)
        {
            return ItemUseResult.Refused();
        }

        inventory.Spend(player, item, 1);
        Tell(player, new ItemUseResponse { Item = item.Item, Gain = new ItemUseGainValue { Items = [] } });

        return ItemUseResult.Used();
    }
}
