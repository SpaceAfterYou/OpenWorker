using OpenWorker.Gameplay.Modules.ItemSetup;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.ItemSetup.Requests;

namespace OpenWorker.DistrictServer.Services;

/// <summary>
/// The requests of the work on items that make one item of others at the NPCs of a city: production, the exchange of
/// materials, disassembly and evolution. What each does: <c>docs/notes-item-setup-handlers.md</c>.
/// </summary>
public sealed class ItemCraftService(
    ItemMakeService makes,
    ItemExchangeService exchanges,
    ItemDisassembleService disassemblies,
    ItemEvolutionService evolutions) :
    IHotspotHandler<ItemSetupMakeRequest>,
    IHotspotHandler<ItemSetupExchangeRequest>,
    IHotspotHandler<ItemSetupDisassembleRequest>,
    IHotspotHandler<ItemSetupDisassembleExRequest>,
    IHotspotHandler<ItemSetupEvolutionRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupMakeRequest request)
    {
        makes.Make(context.Player, request.MakeIndex, request.MakeCount);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupExchangeRequest request)
    {
        exchanges.Exchange(context.Player, request.Index, request.Count);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupDisassembleRequest request)
    {
        disassemblies.Disassemble(context.Player, request.Tool, request.Target);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupDisassembleExRequest request)
    {
        disassemblies.DisassembleMany(context.Player, request.Targets);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemSetupEvolutionRequest request)
    {
        evolutions.Evolve(context.Player, request.Target);

        return ValueTask.CompletedTask;
    }
}
