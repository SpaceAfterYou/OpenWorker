using Arch.Core;

namespace OpenWorker.Gameplay.Modules.Items;

public sealed partial class ItemUseService
{
    /// <summary>
    /// A ticket of the infinite tower clears the count of the day once its entries are used up, and is used up itself.
    /// </summary>
    private ItemUseResult UseTowerTicket(Entity player, ItemInstance item)
    {
        if (tower is null)
        {
            return ItemUseResult.Refused(ItemUseCodes.NoTowerTicket);
        }

        if (tower.UseTicket(player) is { } refused)
        {
            return ItemUseResult.Refused(refused);
        }

        inventory.Spend(player, item, 1);

        return ItemUseResult.Used();
    }
}
