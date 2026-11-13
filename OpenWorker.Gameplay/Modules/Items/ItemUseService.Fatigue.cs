using Arch.Core;
using OpenWorker.Res.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Items;

public sealed partial class ItemUseService
{
    /// <summary>
    /// An item of fatigue adds its value to the fatigue of the day, unless the person has saved fatigue or a full day. The
    /// ones that are not free may be used three times a game day.
    /// </summary>
    private ItemUseResult UseFatigue(Entity player, ItemInstance item, ItemRow row, ItemClassifyRow classify)
    {
        var type = (int)classify.ItemUseType;

        if (classify.Group != ConsumableGroup || fatigue is null)
        {
            return ItemUseResult.Refused();
        }

        if (classify.ItemUseType == ItemUseType.FP && !limits.CanUseKind(player, type))
        {
            return ItemUseResult.Refused(ItemUseCodes.DailyLimit);
        }

        if (row.UseValue == 0)
        {
            return ItemUseResult.Refused();
        }

        if (fatigue.Restore(player, row.UseValue) is { } refusal)
        {
            return ItemUseResult.Refused(refusal);
        }

        inventory.Spend(player, item, 1);
        limits.CountUse(player, type);

        return ItemUseResult.Used();
    }
}
