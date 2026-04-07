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
using OpenWorker.Gameplay.Modules.Items.Components;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.Hotspot.Modules.Shop.Requests;
using OpenWorker.Hotspot.Modules.Shop.Responses;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.DistrictServer.Services;

public sealed class ShopService(
    World world,
    NpcManager npcManager,
    ReadOnlyCollection<NPCRow> npcList,
    ReadOnlyCollection<ShopRow> shopList,
    ReadOnlyCollection<ItemRow> itemList,
    StorageItemFactory itemFactory,
    StorageManager storageManager) :
    IHotspotHandler<ShopBuyRequest>,
    IHotspotHandler<ShopGachaRequest>,
    IHotspotHandler<ShopRepurchaserListRequest>,
    IHotspotHandler<ShopRepurchaserRequest>,
    IHotspotHandler<ShopSellRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, ShopBuyRequest request)
    {
        // var npcEntity = npcManager.List.First(e => e.Get<ActorComponent>().Id == message.Npc.Identifier);
        //
        // var creature = npcEntity.Get<CreatureComponent>();
        //
        // var npc = npcList.First(e => e.Id == creature.Id);
        var shopRow = shopList.First(e => e.Id == request.Index); // e.Field1 == npc.Field10 group
        var itemRow = itemList.First(e => e.Id == shopRow.Item);

        Debug.Assert(shopRow.Count == request.Count);

        var count = (short)shopRow.Count;
        var price = (short)(itemRow.Price * count);

        var created = itemFactory.Create(shopRow.Item, count);

        var response = storageManager.TryAdd(context.Player, created, count);

        if (response.State is false)
        {
            return ValueTask.CompletedTask;
        }

        var session = world.Get<ServerSessionComponent>(context.Player);

        foreach (var info in response.Info)
        {
            var item = ItemDtoFactory.FromEntity(world, created);

            var storage = new StorageValue
            {
                Storage = world.Get<StorageGroupComponent>(response.Storage).Group,
                Slot = info.Slot,
                Item = item
            };

            session.Send(new ShopBuyResponse
            {
                StorageList = [storage],
                Spent = price,
                Currency = ShopCurrency.Gold
            });
        }

        var currency = context.Player.Get<CurrencyComponent>();
        currency.Gold -= price;

        session.Send(new ItemUpdateInvenMoneyResponse
        {
            Total = currency.Gold,
            Bonus = 0,
            Type = MoneyEarnFlow.Normal
        });

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
