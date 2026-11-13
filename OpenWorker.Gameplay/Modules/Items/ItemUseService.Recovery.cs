using Arch.Core;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Items;

public sealed partial class ItemUseService
{
    // The kind of effect in the item row that names a buff and an aura.
    private const int BuffEffect = 1;
    private const int AuraEffect = 2;

    // What a kind of item may do where, in the bits of the use state of its classify row.
    private const int UsableInDistrict = 1;
    private const int UsableInMaze = 2;

    // The calls that get a fallen player up are items of the sub type 2 whose effect is 1 (the player itself).
    private const int ReviveSubType = 2;
    private const int SelfReviveEffect = 1;

    /// <summary>
    /// A potion or the like: in a maze it gives its buff to the person. A place the kind of item is not for and the infinite
    /// tower are refused; so are the items that bring a person back, but for the call that gets the person itself up.
    /// </summary>
    private ItemUseResult UseEffect(Entity player, ItemInstance item, ItemRow row, ItemClassifyRow classify, ItemPlace place)
    {
        if (classify.Group != ConsumableGroup)
        {
            return ItemUseResult.Refused();
        }

        // Where the kind may be used: the place has to take items, and the use state of the kind has to name the place.
        var bit = place.InMaze ? UsableInMaze : UsableInDistrict;

        if (!place.ItemsAllowed || (classify.UseState & bit) == 0)
        {
            return ItemUseResult.Refused(ItemUseCodes.WrongPlace);
        }

        if (place.Infinite)
        {
            return ItemUseResult.Refused(ItemUseCodes.InfiniteTower);
        }

        // A call that gets a person up is used from the window of the fallen: the maze says whether it does.
        if (row.SubType == ReviveSubType && row.EffectType == SelfReviveEffect && revive is not null)
        {
            return revive.Use(player, item);
        }

        // The other items that get a person up are not for this request.
        if (row.SubType != 0)
        {
            return ItemUseResult.Refused(ItemUseCodes.NoRevive);
        }

        var given = row.EffectType switch
        {
            BuffEffect => _field.GiveBuff(player, row.EffectValue),
            AuraEffect => _field.GiveAura(player, row.EffectValue),
            _ => false
        };

        if (!given)
        {
            return ItemUseResult.Refused();
        }

        // The stack may go with its last item: the motion names the slot it was in.
        var slot = item.Slot;

        inventory.Spend(player, item, 1);

        return ItemUseResult.Used(item.Item, slot, motion: true);
    }
}
