using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Login.Requests;

namespace OpenWorker.GateServer.Gameplay.Services;

[HotspotHandler(HotspotHandlerType.Gate)]
public sealed class LoginService(LoginGameplay login, OptionGameplay option) : 
    IHotspotHandler<LoginEnterServerRequest>,
    IHotspotHandler<LoginOptionUpdateRequest>
{
    public async ValueTask OnHandleAsync(ServiceHandleContext context, LoginEnterServerRequest request)
    {
        await login
            .TryJoinAsync(context, request)
            .ConfigureAwait(false);
    }
    
    public async ValueTask OnHandleAsync(ServiceHandleContext context, LoginOptionUpdateRequest request)
    {
        await option
            .PushAsync(context, request)
            .ConfigureAwait(false);
    }
}

// https://youtu.be/UnIhRpIT7nc?list=RDGMEMXdNDEg4wQ96My0DhjI-cIgVMUnIhRpIT7nc