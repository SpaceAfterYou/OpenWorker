using OpenWorker.AuthServer.App;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Login.Requests;

namespace OpenWorker.AuthServer.Gameplay.Services;

[HotspotHandler(HotspotHandlerType.Auth)]
internal sealed class GateService(GateGameplay gameplay) :
    IHotspotHandler<LoginGateListRequest>,
    IHotspotHandler<LoginGateConnectRequest>
{
    public async ValueTask OnHandleAsync(ServiceHandleContext context, LoginGateConnectRequest request)
    {
        await gameplay
            .TryJoinAsync(context, request)
            .ConfigureAwait(false);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, LoginGateListRequest request)
    {
        await gameplay
            .GetListAsync(context, request)
            .ConfigureAwait(false);
    }
}