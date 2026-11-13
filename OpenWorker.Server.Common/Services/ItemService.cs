using Arch.Core;
using OpenWorker.Channel;
using OpenWorker.Gameplay.Modules.HiddenEvents;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Modules.Items.Responses;

namespace OpenWorker.Server.Common.Services;

/// <summary>
/// The inventory in a district and in a maze; a change of what a player wears goes to those who see it.
/// </summary>
public sealed class ItemService(World world, InventoryService inventory, ItemUseService use, IPlayerAudience audience, HiddenEventCounter? hiddenEvents = null)
    : ItemServiceBase(world, inventory, use)
{
    protected override void ItemSpent(Entity player, int item)
    {
        hiddenEvents?.Count(player, HiddenEventTarget.ItemUsed, item, 1);
    }

    protected override void WornChanged(Entity player, ItemMoveBtResponse notice)
    {
        audience.NearbyToOthers(player, notice);
    }

    protected override void ItemUsed(Entity player, ItemUseBtResponse notice)
    {
        audience.Nearby(player, in notice);
    }
}
