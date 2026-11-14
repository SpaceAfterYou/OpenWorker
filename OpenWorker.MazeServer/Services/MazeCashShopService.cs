using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.CashShop;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Shop.Requests;
using OpenWorker.Hotspot.Modules.System.Responses;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// The cash shop is not opened in a maze: the client refuses its window there by itself, and a request that comes anyway is
/// refused with the code the client shows for it. Gacha items still open in a maze.
/// </summary>
public sealed class MazeCashShopService(World world) :
    IHotspotHandler<ShopCashLoadRequest>,
    IHotspotHandler<ShopCashBuyRequest>,
    IHotspotHandler<ShopCashGiftRequest>,
    IHotspotHandler<ShopCashSetRequest>,
    IHotspotHandler<ShopCashSetDelRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, ShopCashLoadRequest request) => Refuse(context, ShopOpcode.CashLoad);

    public ValueTask OnHandleAsync(ServiceHandleContext context, ShopCashBuyRequest request) => Refuse(context, ShopOpcode.CashBuy);

    public ValueTask OnHandleAsync(ServiceHandleContext context, ShopCashGiftRequest request) => Refuse(context, ShopOpcode.CashGift);

    public ValueTask OnHandleAsync(ServiceHandleContext context, ShopCashSetRequest request) => Refuse(context, ShopOpcode.CashSet);

    public ValueTask OnHandleAsync(ServiceHandleContext context, ShopCashSetDelRequest request) => Refuse(context, ShopOpcode.CashSetDel);

    private ValueTask Refuse(ServiceHandleContext context, ShopOpcode command)
    {
        if (world.IsAlive(context.Player))
        {
            world.Get<ServerSessionComponent>(context.Player)
                .Send(new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.Shop, command), (ushort)CashShopCodes.NotHere));
        }

        return ValueTask.CompletedTask;
    }
}
