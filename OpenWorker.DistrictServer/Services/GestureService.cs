using OpenWorker.DistrictServer.Gameplay;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Gestures.Request;

namespace OpenWorker.DistrictServer.Services;

public sealed class GestureService(GestureGameplay gameplay) :
    IHotspotHandler<GestureShowRequest>,
    IHotspotHandler<GestureSlotUpdateRequest>
{
    public async ValueTask OnHandleAsync(ServiceHandleContext context, GestureShowRequest request)
    {
        await gameplay
            .ShowAsync(context, request)
            .ConfigureAwait(false);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, GestureSlotUpdateRequest request)
    {
        await gameplay
            .UpdateAsync(context, request)
            .ConfigureAwait(false);
    }
}