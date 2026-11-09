using OpenWorker.Gameplay.Modules.ItemSetup;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.ItemSetup.Requests;

namespace OpenWorker.DistrictServer.Services;

/// <summary>
/// The work on items at the NPCs of a city: repairs, upgrades, sockets, and the tools that restore, unseal or lower a count of
/// an item. What each request does and what is still missing for the others: <c>docs/notes-item-setup-handlers.md</c>.
/// </summary>
public sealed class ItemSetupService(
    ItemRepairService repairs,
    ItemRestoreService restores,
    ItemSocketService sockets,
    ItemUpgradeService upgrades) :
    IHotspotHandler<ItemSetupMakeRequest>,
    IHotspotHandler<ItemSetupRepairRequest>,
    IHotspotHandler<ItemSetupRepairNpcRequest>,
    IHotspotHandler<ItemSetupRepairEquipRequest>,
    IHotspotHandler<ItemSetupRepairAllRequest>,
    IHotspotHandler<ItemSetupRestoreRequest>,
    IHotspotHandler<ItemSetupUnsealRequest>,
    IHotspotHandler<ItemSetupUseEffectRequest>,
    IHotspotHandler<ItemSetupSocketActiveRequest>,
    IHotspotHandler<ItemSetupSocketEquipRequest>,
    IHotspotHandler<ItemSetupUpgradeRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupMakeRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupRepairRequest request)
    {
        repairs.RepairWithKit(context.Player, request.Kit, request.Target);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupRepairNpcRequest request)
    {
        repairs.RepairOne(context.Player, request.Target);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupRepairEquipRequest request)
    {
        repairs.RepairMany(context.Player, carried: false);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupRepairAllRequest request)
    {
        repairs.RepairMany(context.Player, carried: true);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupRestoreRequest request)
    {
        restores.Restore(context.Player, request.Tool, request.Target);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupUnsealRequest request)
    {
        restores.Unseal(context.Player, request.Tool, request.Target);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupUseEffectRequest request)
    {
        restores.UseEffect(context.Player, request.Tool, request.Target);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupSocketActiveRequest request)
    {
        sockets.Activate(context.Player, request.Target, request.Socket);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupSocketEquipRequest request)
    {
        sockets.Equip(context.Player, request.Target, request.Gems);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupUpgradeRequest request)
    {
        upgrades.Upgrade(context.Player, request.Protect, request.Protection, request.Target);

        return ValueTask.CompletedTask;
    }
}
