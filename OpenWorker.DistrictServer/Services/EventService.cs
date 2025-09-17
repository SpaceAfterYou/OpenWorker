using Arch.Core;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Events.Requests;
using OpenWorker.Hotspot.Modules.Events.Responses;

namespace OpenWorker.DistrictServer.Services;

public sealed class EventService(World world) : 
    IHotspotHandler<EventAttendancePlayTimeRewardRequest>,
    IHotspotHandler<EventAttendanceRewardRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, EventAttendancePlayTimeRewardRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);
        
        session.Send(new EventAttendancePlayTimeRewardResponse());
        
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, EventAttendanceRewardRequest request)
    {
        // TODO: Do something 
        
        return ValueTask.CompletedTask;
    }
}