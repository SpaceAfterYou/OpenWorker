using System.Collections.ObjectModel;
using Arch.Core;
using Arch.Core.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.Hotspot.Modules.Shop.Requests;
using OpenWorker.Hotspot.Modules.Shop.Responses;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.DistrictServer.Services;

public sealed class CashShopService(World world, ReadOnlyCollection<CashShopRow> cashShop) :
    IHotspotHandler<ShopCashBuyRequest>,
    IHotspotHandler<ShopCashGiftRequest>,
    IHotspotHandler<ShopCashLoadRequest>,
    IHotspotHandler<ShopCashSetDelRequest>,
    IHotspotHandler<ShopCashSetRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, ShopCashBuyRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ShopCashGiftRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ShopCashLoadRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);
        
        var date = DateTimeOffset.UtcNow;
        
        session.Send(new ShopCashLoadResponse([
            new CashItemValue
            {
                Index = 1,
                SellActive = 1,
                Item = 837000044,
                Level = 1,
                Order = 1,
                NeedSlot = 1,
                CashInfo = (short)CashHopInfoFlags.IconHot,
                LimitTime1 = date.AddDays(1),
                LimitTime2 = date.AddDays(2),
                SellCount = 1,
                Billing = 9,
                CashInfoList =
                [
                    new CashInfoValue
                    {
                        Item = 837000044,
                        Count = 1,
                        BasePrice = 100,
                        Price = 200
                    }
                ]
            }
        ]));

        // session.Send(new ShopBannerLoadResponse());

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ShopCashSetDelRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ShopCashSetRequest request)
    {
        return ValueTask.CompletedTask;
    }
}