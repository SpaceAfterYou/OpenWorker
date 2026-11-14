using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.CashShop;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Shop.Requests;
using OpenWorker.Hotspot.Modules.Shop.Responses;
using OpenWorker.Hotspot.Modules.System.Responses;

namespace OpenWorker.Server.Common.Services;

/// <summary>
/// Opens gacha items of the bags, in a district and in a maze, whether the cash shop is open or not: a gacha is an item the
/// player holds. Each one gives one item of its draw, told to the gacha window. Everything happens in the inventory of the
/// player, which the flush writes.
/// </summary>
public sealed class GachaService(World world, CashShopGacha gacha, InventoryService inventory, TimeProvider? time = null)
    : IHotspotHandler<ShopGachaRequest>
{
    private TimeProvider Time => time ?? TimeProvider.System;

    public async ValueTask OnHandleAsync(ServiceHandleContext context, ShopGachaRequest request)
    {
        var player = context.Player;

        if (request.Count is 0 or > CashShopGacha.MaxCount || !gacha.TryGetGacha(request.Item, out var box))
        {
            Fail(context, CashShopCodes.NotGacha);
            return;
        }

        if (inventory.CountOf(player, request.Item) < request.Count)
        {
            Fail(context, CashShopCodes.CountMismatch);
            return;
        }

        if (!CashShopGacha.Open(box, Time.GetLocalNow().DateTime))
        {
            Fail(context, CashShopCodes.GachaClosed);
            return;
        }

        if (gacha.Draw(box, request.Count) is not { } results)
        {
            Fail(context, CashShopCodes.GachaFailed);
            return;
        }

        var given = results.Select(e => (e.Item, 1)).ToList();

        await inventory.EnsureSerialsAsync(given);

        // The wait let other work run: the bags are counted again.
        if (!world.IsAlive(player))
        {
            return;
        }

        if (inventory.CountOf(player, request.Item) < request.Count)
        {
            Fail(context, CashShopCodes.CountMismatch);
            return;
        }

        if (!inventory.CanGiveAll(player, given))
        {
            Fail(context, CashShopCodes.NoRoom);
            return;
        }

        inventory.TryTake(player, request.Item, request.Count);

        foreach (var (item, count) in given)
        {
            inventory.TryGive(player, item, count);
        }

        Send(context, new ShopGachaResResponse { Results = results });
    }

    private void Send<T>(ServiceHandleContext context, T message) where T : IResponseHotspotMessage
    {
        if (world.IsAlive(context.Player))
        {
            world.Get<ServerSessionComponent>(context.Player).Send(message);
        }
    }

    private void Fail(ServiceHandleContext context, int code)
    {
        Send(context, new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.Shop, ShopOpcode.Gacha), (ushort)code));
    }
}
