using Arch.Core;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Requests;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.System.Responses;

namespace OpenWorker.Channel;

/// <summary>
/// The requests of the client about its inventory. A district and a maze answer them the same way.
/// </summary>
public abstract class ItemServiceBase(World world, InventoryService inventory, ItemUseService use) :
    IHotspotHandler<ItemInventoryInfoRequest>,
    IHotspotHandler<ItemBankInfoRequest>,
    IHotspotHandler<ItemMoveRequest>,
    IHotspotHandler<ItemCombineRequest>,
    IHotspotHandler<ItemDivideRequest>,
    IHotspotHandler<ItemBreakRequest>,
    IHotspotHandler<ItemUseRequest>,
    IHotspotHandler<ItemAddSlotRequest>,
    IHotspotHandler<ItemLineUpRequest>,
    IHotspotHandler<ItemUpdateQuickslotCardRequest>,
    IHotspotHandler<ItemUpdateQuickslotItemRequest>,
    IHotspotHandler<ItemAppearanceEquipRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemInventoryInfoRequest request)
    {
        var player = context.Player;

        if (inventory.InventoryOf(player) is not { } bag)
        {
            return ValueTask.CompletedTask;
        }

        var session = world.Get<ServerSessionComponent>(player);

        session.Send(inventory.DescribeSlots(player, request.IdentifierList));
        session.Send(new ItemInventoryInfoResponse { List = inventory.ListItems(player, request.IdentifierList) });

        // The client asks for the bags once it is in the game, which is also when it wants its quick slots.
        if (request.IdentifierList.Contains(StorageGroup.Common))
        {
            session.Send(new ItemLoadQuickslotResponse { Cards = bag.QuickSlotCards, Items = bag.QuickSlotItems });
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemBankInfoRequest request)
    {
        var player = context.Player;

        if (inventory.InventoryOf(player) is null)
        {
            return ValueTask.CompletedTask;
        }

        var session = world.Get<ServerSessionComponent>(player);

        session.Send(inventory.DescribeSlots(player, request.IdentifierList));
        session.Send(new ItemBankInfoResponse { List = inventory.ListItems(player, request.IdentifierList) });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemMoveRequest request)
    {
        inventory.Move(context.Player, request.Src, request.Dest);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemCombineRequest request)
    {
        inventory.Combine(context.Player, request.Src, request.Dest, request.Count);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemDivideRequest request)
    {
        inventory.Divide(context.Player, request.Src, request.DestStorage, request.DestIndex, request.Count);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemBreakRequest request)
    {
        inventory.Break(context.Player, request.Storage, request.Index, request.Serial, request.Count);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemUseRequest request)
    {
        if (!use.Use(context.Player, request.Storage, request.Slot, request.Serial.Number))
        {
            world.Get<ServerSessionComponent>(context.Player).Send(new ErrorMessageResponse(
                new MessageOpcode(GroupOpcode.Item, ItemOpcode.Use),
                InventoryService.Refused));
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemAddSlotRequest request)
    {
        inventory.GradeUp(context.Player, request.Storage);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemLineUpRequest request)
    {
        inventory.LineUp(context.Player, request.Storage);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemUpdateQuickslotCardRequest request)
    {
        if (inventory.InventoryOf(context.Player) is { } bag)
        {
            bag.QuickSlotCards = request.Cards;
            bag.MarkState();

            world.Get<ServerSessionComponent>(context.Player).Send(new ItemUpdateQuickslotCardResponse { Cards = request.Cards });
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemUpdateQuickslotItemRequest request)
    {
        if (inventory.InventoryOf(context.Player) is { } bag)
        {
            bag.QuickSlotItems = request.Items;
            bag.MarkState();

            world.Get<ServerSessionComponent>(context.Player).Send(new ItemUpdateQuickslotItemResponse { Items = request.Items });
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// The client has appearance items, which this server does not have yet.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemAppearanceEquipRequest request)
    {
        return ValueTask.CompletedTask;
    }
}
