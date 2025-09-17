using Arch.Core;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.SoulMetry.Requests;

namespace OpenWorker.DistrictServer.Services;

[HotspotHandler(HotspotHandlerType.District)]
public sealed class SoulMetryService : IHotspotHandler<SoulCompleteRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, SoulCompleteRequest request)
    {
        return ValueTask.CompletedTask;
    }
}