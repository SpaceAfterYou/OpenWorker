using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.System.Requests;

namespace OpenWorker.GateServer.Gameplay.Services;

[HotspotHandler(HotspotHandlerType.Gate)]
public sealed class SystemService(OptionGameplay option) :
    IHotspotHandler<SystemOptionUpdateRequest>,
    IHotspotHandler<SystemXignCodeRequest>
{
    public async ValueTask OnHandleAsync(ServiceHandleContext context, SystemOptionUpdateRequest request)
    {
        await option
            .PushAsync(context, request.OptionList)
            .ConfigureAwait(false);
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SystemXignCodeRequest request)
    {
        return ValueTask.CompletedTask;
    }
}
