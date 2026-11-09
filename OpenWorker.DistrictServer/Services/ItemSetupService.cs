using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.ItemSetup;
using OpenWorker.Gameplay.Modules.Npcs;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.ItemSetup.Requests;

namespace OpenWorker.DistrictServer.Services;

/// <summary>
/// The work on items at the NPCs of a city: repairs, upgrades, sockets, broaches, akashic cards, and the tools that restore,
/// unseal or lower a count of an item. A request that names an NPC is served only by an NPC of the city near the player that
/// offers the service (<see cref="ServiceNpcs"/>). What each request does and what is still missing for the others:
/// <c>docs/notes-item-setup-handlers.md</c>, <c>docs/notes-npc-services.md</c>.
/// </summary>
public sealed class ItemSetupService(
    ItemRepairService repairs,
    ItemRestoreService restores,
    ItemSocketService sockets,
    ItemUpgradeService upgrades,
    AkashicService akashic,
    ItemBroachService broaches,
    ServiceNpcs npcs) :
    IHotspotHandler<ItemSetupRepairRequest>,
    IHotspotHandler<ItemSetupRepairNpcRequest>,
    IHotspotHandler<ItemSetupRepairEquipRequest>,
    IHotspotHandler<ItemSetupRepairAllRequest>,
    IHotspotHandler<ItemSetupRestoreRequest>,
    IHotspotHandler<ItemSetupUnsealRequest>,
    IHotspotHandler<ItemSetupUseEffectRequest>,
    IHotspotHandler<ItemSetupSocketActiveRequest>,
    IHotspotHandler<ItemSetupSocketEquipRequest>,
    IHotspotHandler<ItemSetupUpgradeRequest>,
    IHotspotHandler<ItemSetupAkashicMakeRequest>,
    IHotspotHandler<ItemSetupAkashicMakeExRequest>,
    IHotspotHandler<ItemSetupAkashicComposeRequest>,
    IHotspotHandler<ItemSetupAkashicDisassembleRequest>,
    IHotspotHandler<ItemSetupBroachEquipRequest>,
    IHotspotHandler<ItemSetupBroachActiveRequest>,
    IHotspotHandler<ItemSetupBroachComposeRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupRepairRequest request)
    {
        repairs.RepairWithKit(context.Player, request.Kit, request.Target);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupRepairNpcRequest request)
    {
        if (npcs.TryServe(context.Player, request.Npc, NpcFunction.Shop, Opcode(ItemSetupOpcode.RepairNpc), ServiceNpcCodes.Repair, out var npc))
        {
            repairs.RepairOne(context.Player, request.Target, npc);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupRepairEquipRequest request)
    {
        if (npcs.TryServe(context.Player, request.Npc, NpcFunction.Shop, Opcode(ItemSetupOpcode.RepairEquip), ServiceNpcCodes.Repair, out var npc))
        {
            repairs.RepairMany(context.Player, carried: false, npc);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupRepairAllRequest request)
    {
        if (npcs.TryServe(context.Player, request.Npc, NpcFunction.Shop, Opcode(ItemSetupOpcode.RepairAll), ServiceNpcCodes.Repair, out var npc))
        {
            repairs.RepairMany(context.Player, carried: true, npc);
        }

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
        if (npcs.TryServe(context.Player, request.Npc, NpcFunction.Upgrade, Opcode(ItemSetupOpcode.Upgrade), ServiceNpcCodes.Upgrade, out var npc))
        {
            upgrades.Upgrade(context.Player, request.Protect, request.Protection, request.Target, npc);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupAkashicMakeRequest request)
    {
        if (npcs.TryServe(context.Player, request.Npc, NpcFunction.None, Opcode(ItemSetupOpcode.AkashicMake), ServiceNpcCodes.Akashic, out _))
        {
            akashic.Make(context.Player, ItemSetupOpcode.AkashicMake, request.Index, request.Material, []);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupAkashicMakeExRequest request)
    {
        if (npcs.TryServe(context.Player, request.Npc, NpcFunction.None, Opcode(ItemSetupOpcode.AkashicMakeEx), ServiceNpcCodes.Akashic, out _))
        {
            akashic.Make(context.Player, ItemSetupOpcode.AkashicMakeEx, request.Index, request.Material, request.Extras);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupAkashicComposeRequest request)
    {
        if (npcs.TryServe(context.Player, request.Npc, NpcFunction.AkashicMix, Opcode(ItemSetupOpcode.AkashicCompose), ServiceNpcCodes.AkashicCompose, out _))
        {
            akashic.Compose(context.Player, request.Target, request.Materials);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupAkashicDisassembleRequest request)
    {
        if (npcs.TryServe(context.Player, request.Npc, NpcFunction.None, Opcode(ItemSetupOpcode.AkashicDisassemble), ServiceNpcCodes.Akashic, out _))
        {
            akashic.Disassemble(context.Player, request.Card);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupBroachEquipRequest request)
    {
        broaches.Equip(context.Player, request.Target, request.Broach, request.Place);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupBroachActiveRequest request)
    {
        broaches.Activate(context.Player, request.Target, request.Place);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupBroachComposeRequest request)
    {
        broaches.Compose(context.Player, request.Tool, request.Materials);

        return ValueTask.CompletedTask;
    }

    private static MessageOpcode Opcode(ItemSetupOpcode command) => MessageOpcode.From(GroupOpcode.ItemSetup, command);
}
