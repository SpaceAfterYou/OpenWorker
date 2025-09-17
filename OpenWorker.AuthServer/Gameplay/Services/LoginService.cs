using OpenWorker.AuthServer.App;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Login.Requests;

namespace OpenWorker.AuthServer.Gameplay.Services;

[HotspotHandler(HotspotHandlerType.Auth)]
internal sealed class LoginService(LoginGameplay gameplay) :  
    IHotspotHandler<LoginAuthRequest>,
    IHotspotHandler<LoginNextHumanNetworkRequest>
{
    public async ValueTask OnHandleAsync(ServiceHandleContext context, LoginAuthRequest request)
    {
        await gameplay
            .TryJoinAsync(context, request)
            .ConfigureAwait(false);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, LoginNextHumanNetworkRequest request)
    {
        await gameplay
            .TryJoinAsync(context, request)
            .ConfigureAwait(false);
    }
}