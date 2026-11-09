using OpenWorker.Gameplay.Modules.DailyMissions;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.DailyMissions.Requests;

namespace OpenWorker.Server.Common.Services;

public sealed class DailyMissionService(DailyMissionManager manager) :
    IHotspotHandler<DailyMissionAcceptRequest>,
    IHotspotHandler<DailyMissionHelperRequest>
{
    public async ValueTask OnHandleAsync(ServiceHandleContext context, DailyMissionAcceptRequest request)
    {
        await manager.AcceptAsync(context.Player, request.Mission, context.CancellationToken);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, DailyMissionHelperRequest request)
    {
        await manager.SetHelperAsync(context.Player, request.Mission, request.Helper, context.CancellationToken);
    }
}
