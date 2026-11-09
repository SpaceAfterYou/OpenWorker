using OpenWorker.Gameplay.Modules.ItemSetup;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.ItemSetup.Requests;

namespace OpenWorker.DistrictServer.Services;

/// <summary>
/// The work on items at the NPCs of a city: repairs so far. The other requests of the group are listed in
/// <c>docs/notes-itemsetup.md</c> with what is missing for them.
/// </summary>
public sealed class ItemSetupService(ItemRepairService repairs) :
    IHotspotHandler<ItemSetupMakeRequest>,
    IHotspotHandler<ItemSetupRepairRequest>,
    IHotspotHandler<ItemSetupRepairNpcRequest>,
    IHotspotHandler<ItemSetupRepairEquipRequest>,
    IHotspotHandler<ItemSetupRepairAllRequest>
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
}
