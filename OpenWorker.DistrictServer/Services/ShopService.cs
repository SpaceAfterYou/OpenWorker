using System.Collections.ObjectModel;
using System.Diagnostics;
using Arch.Core;
using Arch.Core.Extensions;
using OpenWorker.DistrictServer.Server;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Gameplay;
using OpenWorker.Hotspot.Modules.Items;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Hotspot.Modules.Quests.Enums;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.Hotspot.Modules.Shop.Requests;
using OpenWorker.Hotspot.Modules.Shop.Responses;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.DistrictServer.Services;

public sealed class ShopService(
    World world,
    NpcManager npcManager,
    ReadOnlyCollection<NPCRow> npcList,
    ReadOnlyCollection<ShopRow> shopList,
    ItemCatalog items,
    InventoryService inventory,
    QuestManager? quests = null) :
    IHotspotHandler<ShopBuyRequest>,
    IHotspotHandler<ShopGachaRequest>,
    IHotspotHandler<ShopRepurchaserListRequest>,
    IHotspotHandler<ShopRepurchaserRequest>,
    IHotspotHandler<ShopSellRequest>
{
    private readonly RowIndex<int, ShopRow> _shop = shopList.IndexBy(e => e.Id);

    public ValueTask OnHandleAsync(ServiceHandleContext context, ShopBuyRequest request)
    {
        // A line of a shop or an item the tables do not have buys nothing, as a purse that is too light does.
        if (!_shop.TryGet(request.Index, out var shopRow) || !items.TryGet(shopRow.Item, out var itemRow))
        {
            return ValueTask.CompletedTask;
        }

        Debug.Assert(shopRow.Count == request.Count);

        var count = shopRow.Count;
        var price = (long)itemRow.Price * count;

        if (inventory.Gold(context.Player) < price || !inventory.TryGive(context.Player, shopRow.Item, count, out var changes, notify: false))
        {
            return ValueTask.CompletedTask;
        }

        var bag = inventory.InventoryOf(context.Player)!;
        var session = world.Get<ServerSessionComponent>(context.Player);

        session.Send(new ShopBuyResponse
        {
            StorageList = changes
                .Select(e => new StorageValue { Storage = e.Storage, Slot = e.Slot, Item = bag.At(e.Storage, e.Slot)!.ToValue() })
                .ToArray(),
            Spent = (short)price,
            Currency = ShopCurrency.Gold
        });

        inventory.TryAddGold(context.Player, -price);

        // A purchase counts once for the quests that ask to buy the item.
        quests?.OnEvent(context.Player, QuestConditionType.Buy, QuestConditionTarget.Item, shopRow.Item);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ShopGachaRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ShopRepurchaserListRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ShopRepurchaserRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ShopSellRequest request)
    {
        return ValueTask.CompletedTask;
    }
}
