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
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Helpers.Types;
using OpenWorker.Domain.Components;

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
            session.Send(new ItemAkashicRecordLoadResponse { Records = bag.AkashicRecords.Values() });

            // And the gems and broaches of all the items, which the items themselves only count.
            if (inventory.ListSockets(player) is { Length: > 0 } sockets)
            {
                session.Send(new ItemSocketLoadResponse { Sockets = sockets });
            }

            if (inventory.ListBroaches(player) is { Length: > 0 } broaches)
            {
                session.Send(new ItemBroachLoadResponse { Broaches = broaches });
            }
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
        var bag = inventory.InventoryOf(context.Player);
        var moving = bag?.At(request.Src.Storage, request.Src.Index);

        inventory.Move(context.Player, request.Src, request.Dest);

        // The others see what the player wears: the worn slots of a move that took place.
        if (bag is null || moving is null || bag.At(request.Dest.Storage, request.Dest.Index) != moving)
        {
            return ValueTask.CompletedTask;
        }

        var worn = new List<StorageValue>(2);

        foreach (var (storage, slot) in (ReadOnlySpan<(StorageGroup, short)>)[(request.Src.Storage, request.Src.Index), (request.Dest.Storage, request.Dest.Index)])
        {
            if (IsWorn(storage))
            {
                worn.Add(new StorageValue { Storage = storage, Slot = slot, Item = bag.At(storage, slot)?.ToValue() ?? HelperValue.NoItem });
            }
        }

        if (worn.Count > 0)
        {
            WornChanged(context.Player, new ItemMoveBtResponse { Actor = world.Get<ActorComponent>(context.Player).Identifier, Slots = worn });
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// The places whose items the others see on the player: the look, the gear (the weapon among it) and the costume look.
    /// </summary>
    private static bool IsWorn(StorageGroup storage) => storage is StorageGroup.ShapeEquip or StorageGroup.AbilityEquip or StorageGroup.LookEquip;

    /// <summary>
    /// Tells the players who see the player what it now wears; the server knows who they are.
    /// </summary>
    protected abstract void WornChanged(Entity player, ItemMoveBtResponse notice);

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

    public async ValueTask OnHandleAsync(ServiceHandleContext context, ItemUseRequest request)
    {
        if (!await use.UseAsync(context.Player, request.Storage, request.Slot, request.Serial.Number, context.CancellationToken))
        {
            world.Get<ServerSessionComponent>(context.Player).Send(new ErrorMessageResponse(
                MessageOpcode.From(GroupOpcode.Item, ItemOpcode.Use),
                InventoryService.Refused));
        }
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
            bag.MarkChanged();

            world.Get<ServerSessionComponent>(context.Player).Send(new ItemUpdateQuickslotCardResponse { Cards = request.Cards });
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemUpdateQuickslotItemRequest request)
    {
        if (inventory.InventoryOf(context.Player) is { } bag)
        {
            bag.QuickSlotItems = request.Items;
            bag.MarkChanged();

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
