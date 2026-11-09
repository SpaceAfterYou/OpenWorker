using OpenWorker.Gameplay.Modules.SoulMetry;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.SoulMetry.Requests;

namespace OpenWorker.DistrictServer.Services;

public sealed class SoulMetryService(SoulMetryManager manager) : IHotspotHandler<SoulCompleteRequest>
{
    public async ValueTask OnHandleAsync(ServiceHandleContext context, SoulCompleteRequest request)
    {
        await manager.CompleteAsync(context.Player, request.Identifier, context.CancellationToken);
    }
}
