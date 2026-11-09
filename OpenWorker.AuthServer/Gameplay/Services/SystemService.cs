using OpenWorker.AuthServer.App;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Login.Requests;
using OpenWorker.Hotspot.Modules.System.Requests;

namespace OpenWorker.AuthServer.Gameplay.Services;

internal sealed class SystemService(LoginGameplay gameplay) :
    IHotspotHandler<LoginOptionUpdateRequest>,
    IHotspotHandler<SystemOptionUpdateRequest>,
    IHotspotHandler<SystemXignCodeRequest>
{
    public async ValueTask OnHandleAsync(ServiceHandleContext context, LoginOptionUpdateRequest request)
    {
        await gameplay
            .SaveOptionsAsync(context, request.Values);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, SystemOptionUpdateRequest request)
    {
        await gameplay
            .SaveOptionsAsync(context, request.OptionList);
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SystemXignCodeRequest request)
    {
        return ValueTask.CompletedTask;
    }
}
