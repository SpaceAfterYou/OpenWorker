using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.ItemSetup;
using OpenWorker.Gameplay.Modules.Npcs;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.ItemSetup.Requests;

namespace OpenWorker.DistrictServer.Services;

/// <summary>
/// The requests of the work on items that make one item of others at the NPCs of a city: production, the exchange of
/// materials, disassembly and evolution. A request that names an NPC is served only by an NPC of the city near the player
/// that offers it (<see cref="ServiceNpcs"/>). What each does: <c>docs/notes-item-setup-handlers.md</c>.
/// </summary>
public sealed class ItemCraftService(
    ItemMakeService makes,
    ItemExchangeService exchanges,
    ItemDisassembleService disassemblies,
    ItemEvolutionService evolutions,
    ServiceNpcs npcs) :
    IHotspotHandler<ItemSetupMakeRequest>,
    IHotspotHandler<ItemSetupExchangeRequest>,
    IHotspotHandler<ItemSetupDisassembleRequest>,
    IHotspotHandler<ItemSetupDisassembleExRequest>,
    IHotspotHandler<ItemSetupEvolutionRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupMakeRequest request)
    {
        // An NPC makes the recipes of the group its production names.
        if (npcs.TryServe(context.Player, request.Npc, NpcFunction.Production, Opcode(ItemSetupOpcode.Make), ServiceNpcCodes.Make, out var npc,
                makes.GroupOf(request.MakeIndex)))
        {
            makes.Make(context.Player, request.MakeIndex, request.MakeCount, npc);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupExchangeRequest request)
    {
        // An exchange is made at an NPC that exchanges and is of the kind its row names.
        if (npcs.TryServe(context.Player, request.Npc, NpcFunction.Exchange, Opcode(ItemSetupOpcode.Exchange), ServiceNpcCodes.Exchange, out var npc,
                also: exchanges.FunctionOf(request.Index)))
        {
            exchanges.Exchange(context.Player, request.Index, request.Count, npc);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupDisassembleRequest request)
    {
        disassemblies.Disassemble(context.Player, request.Tool, request.Target);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupDisassembleExRequest request)
    {
        if (npcs.TryServe(context.Player, request.Npc, NpcFunction.None, Opcode(ItemSetupOpcode.DisassembleEx), ServiceNpcCodes.Disassemble, out _))
        {
            disassemblies.DisassembleMany(context.Player, request.Targets);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupEvolutionRequest request)
    {
        if (npcs.TryServe(context.Player, request.Npc, NpcFunction.Upgrade, Opcode(ItemSetupOpcode.Evolution), ServiceNpcCodes.Upgrade, out var npc))
        {
            evolutions.Evolve(context.Player, request.Target, npc);
        }

        return ValueTask.CompletedTask;
    }

    private static MessageOpcode Opcode(ItemSetupOpcode command) => MessageOpcode.From(GroupOpcode.ItemSetup, command);
}
