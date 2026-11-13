using OpenWorker.Gameplay.Modules.Shop;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Shop.Requests;

namespace OpenWorker.DistrictServer.Services;

/// <summary>
/// The shops of the NPCs of a city: buying, selling and buying back (<see cref="NpcShopService"/>).
/// </summary>
public sealed class ShopService(NpcShopService shops) :
    IHotspotHandler<ShopBuyRequest>,
    IHotspotHandler<ShopGachaRequest>,
    IHotspotHandler<ShopRepurchaserListRequest>,
    IHotspotHandler<ShopRepurchaserRequest>,
    IHotspotHandler<ShopSellRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, ShopBuyRequest request)
    {
        shops.Buy(context.Player, request.Npc, request.Index, request.Count);

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// A draw of the cash shop, which this server does not run.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, ShopGachaRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ShopRepurchaserListRequest request)
    {
        shops.RepurchaseList(context.Player, request.Npc);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ShopRepurchaserRequest request)
    {
        shops.Repurchase(context.Player, request.Npc, request.Item);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ShopSellRequest request)
    {
        shops.Sell(context.Player, request.Npc, request.Entries);

        return ValueTask.CompletedTask;
    }
}
