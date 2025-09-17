using Arch.Core;
using OpenWorker.DistrictServer.Server.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.System.Requests;
using OpenWorker.Hotspot.Modules.System.Responses;

namespace OpenWorker.DistrictServer.Services;

[HotspotHandler(HotspotHandlerType.District)]
public sealed class SystemService(World world) :
    IHotspotHandler<SystemOptionUpdateRequest>,
    IHotspotHandler<SystemXignCodeRequest>,
    IHotspotHandler<SystemKeepAliveRequest>,
    IHotspotHandler<SystemPingRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, SystemKeepAliveRequest request)
    {
        var component = world.Get<KeepAliveComponent>(context.Player);
            
        world.Set(context.Player, component with { LastTickCount = new TimeSpan(request.TickCount) });
            
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SystemOptionUpdateRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SystemPingRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);
        
        session.Send(new SystemPingResponse(request.TickCount));
        
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SystemXignCodeRequest request)
    {
        return ValueTask.CompletedTask;
    }
}