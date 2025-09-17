using Arch.Core;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.DailyMissions.Requests;

namespace OpenWorker.DistrictServer.Services;

[HotspotHandler(HotspotHandlerType.District)]
public sealed class DailyMissionService :
    IHotspotHandler<DailyMissionAcceptRequest>,
    IHotspotHandler<DailyMissionHelperRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, DailyMissionAcceptRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, DailyMissionHelperRequest request)
    {
        return ValueTask.CompletedTask;
    }
}