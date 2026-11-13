using Arch.Core;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Items;

public sealed partial class ItemUseService
{
    /// <summary>
    /// An akashic record card goes into the book of the person.
    /// </summary>
    private ItemUseResult UseAkashic(Entity player, ItemInstance item, ItemClassifyRow classify)
    {
        if (akashic is null || !akashic.Register(player, item))
        {
            return ItemUseResult.Refused();
        }

        limits.CountUse(player, (int)classify.ItemUseType);

        return ItemUseResult.Used();
    }
}
