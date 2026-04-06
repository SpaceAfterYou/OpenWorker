using Arch.Core;
using OpenWorker.DistrictServer.Server;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Gameplay;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Hotspot.Modules.Items.Requests;
using OpenWorker.Hotspot.Modules.Items.Responses;

namespace OpenWorker.DistrictServer.Services;

public sealed class ItemService(
    World world,
    StorageItemFactory itemFactory,
    StorageManager storageManager
) :
    IHotspotHandler<ItemInventoryInfoRequest>,
    IHotspotHandler<ItemMoveRequest>,
    IHotspotHandler<ItemCombineRequest>,
    IHotspotHandler<ItemDivideRequest>,
    IHotspotHandler<ItemBreakRequest>,
    IHotspotHandler<ItemAddSlotRequest>,
    IHotspotHandler<ItemBankInfoRequest>,
    IHotspotHandler<ItemUseRequest>,
    IHotspotHandler<ItemLineUpRequest>,
    IHotspotHandler<ItemUpdateQuickslotCardRequest>,
    IHotspotHandler<ItemUpdateQuickslotItemRequest>,
    IHotspotHandler<ItemMazeRewardItemRequest>,
    IHotspotHandler<ItemAppearanceEquipRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemAddSlotRequest request)
    {
        storageManager.GradeUp(context.GetPlayerEntity(), request.Storage);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemAppearanceEquipRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemBankInfoRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.GetPlayerEntity());

        session.Send(ItemDtoFactory.CreateOpenSlotInfoResponse(world, context.GetPlayerEntity(), request.IdentifierList));

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemBreakRequest request)
    {
        storageManager.Break(context.GetPlayerEntity(), request.Storage, request.Index, request.Count);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemCombineRequest request)
    {
        storageManager.Combine(context.GetPlayerEntity(), request.Src.Storage, request.Dest.Storage, request.Src.Index, request.Dest.Index, request.Count);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemDivideRequest request)
    {
        storageManager.Divide(context.GetPlayerEntity(), request.Src.Storage, request.DestStorage, request.Src.Index, request.DestIndex, request.Count);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemInventoryInfoRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.GetPlayerEntity());

        session.Send(ItemDtoFactory.CreateOpenSlotInfoResponse(world, context.GetPlayerEntity(), request.IdentifierList));
        session.Send(ItemDtoFactory.CreateItemInventoryInfoResponse(world, context.GetPlayerEntity(), request.IdentifierList));

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemLineUpRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemMazeRewardItemRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemMoveRequest request)
    {
        storageManager.Move(context.GetPlayerEntity(), request.Src.Storage, request.Dest.Storage, request.Src.Index, request.Dest.Index);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemUpdateQuickslotCardRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemUpdateQuickslotItemRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemUseRequest request)
    {
        return ValueTask.CompletedTask;
    }
}
