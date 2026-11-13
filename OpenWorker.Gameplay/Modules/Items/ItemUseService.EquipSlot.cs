using Arch.Core;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Items;

public sealed partial class ItemUseService
{
    // The slot types of the items that open a memory place of the equipment: the first memory place, then the ten places.
    private const int FirstMemorySlotType = 101;
    private const int MemorySlots = 10;

    /// <summary>
    /// An item of a place of the equipment opens that place, if it is not open yet. The item comes from the common bag.
    /// </summary>
    private ItemUseResult OpenEquipSlot(Entity player, ItemInstance item, ItemClassifyRow classify)
    {
        var bit = classify.SlotType - FirstMemorySlotType;

        if (item.Storage != StorageGroup.Common || bit is < 0 or >= MemorySlots || limits.IsEquipSlotOpen(player, bit))
        {
            return ItemUseResult.Refused();
        }

        inventory.Spend(player, item, 1);
        limits.OpenEquipSlot(player, bit);

        return ItemUseResult.Used();
    }
}
