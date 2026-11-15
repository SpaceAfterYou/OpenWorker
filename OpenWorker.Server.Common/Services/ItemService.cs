using Arch.Core;
using OpenWorker.Channel;
using OpenWorker.Gameplay.Modules.HiddenEvents;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persons;
using OpenWorker.Gameplay.Modules.SocialItems;
using OpenWorker.Hotspot.Modules.Items.Requests;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Modules.Items.Responses;

namespace OpenWorker.Server.Common.Services;

/// <summary>
/// The inventory in a district and in a maze; a change of what a player wears goes to those who see it. In a district the stack a
/// player's social object was put down from stays in its storage while the object stands. An item used from the bag counts once
/// for the hidden event of the maze and opens the titles of its use.
/// </summary>
public sealed class ItemService(
    World world,
    InventoryService inventory,
    ItemUseService use,
    IPlayerAudience audience,
    HiddenEventCounter? hiddenEvents = null,
    SocialItems? socialItems = null,
    OpenTitleManager? openTitles = null)
    : ItemServiceBase(world, inventory, use)
{
    protected override ushort MoveHeldBack(Entity player, MoveItemValue src, MoveItemValue dest)
    {
        return socialItems?.HoldsStack(player, src, dest) == true ? SocialItemCodes.ItemInUse : (ushort)0;
    }

    protected override void ItemSpent(Entity player, int item)
    {
        hiddenEvents?.Count(player, HiddenEventTarget.ItemUsed, item, 1);
        openTitles?.Update(player, OpenTitleManager.ByItemUse, item);
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
