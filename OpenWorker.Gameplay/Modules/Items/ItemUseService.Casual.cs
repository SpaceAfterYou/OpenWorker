using Arch.Core;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Items;

public sealed partial class ItemUseService
{
    // The effect of an item that names the maze whose entries it renews.
    private const short MazeEffect = 9;

    /// <summary>
    /// An item that renews the entries of the maze its effect names, once they are used up today; it is used up itself.
    /// </summary>
    private ItemUseResult RenewMaze(Entity player, ItemInstance item, ItemRow row)
    {
        if (entryLimits is null || row.EffectType != MazeEffect || !entryLimits.Renew(player, row.EffectValue))
        {
            return ItemUseResult.Refused();
        }

        inventory.Spend(player, item, 1);

        return ItemUseResult.Used();
    }

    /// <summary>
    /// An item that gives back one entry of every casual raid entered today; it is used up itself.
    /// </summary>
    private ItemUseResult TakeBackCasualEntry(Entity player, ItemInstance item)
    {
        if (entryLimits is null || !entryLimits.TakeBackCasualEntry(player))
        {
            return ItemUseResult.Refused();
        }

        inventory.Spend(player, item, 1);

        return ItemUseResult.Used();
    }
}
