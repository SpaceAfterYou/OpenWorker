using OpenWorker.Gameplay.Modules.Attendance;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Events.Requests;

namespace OpenWorker.DistrictServer.Services;

/// <summary>
/// The rewards of attendance go to the person by themselves, so the requests of the client only make the server look again.
/// </summary>
public sealed class EventService(AttendanceManager attendance) :
    IHotspotHandler<EventAttendancePlayTimeRewardRequest>,
    IHotspotHandler<EventAttendanceRewardRequest>
{
    public async ValueTask OnHandleAsync(ServiceHandleContext context, EventAttendancePlayTimeRewardRequest request)
    {
        await attendance.CountPlayTimeAsync(context.Player, context.CancellationToken);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, EventAttendanceRewardRequest request)
    {
        await attendance.RefreshAsync(context.Player, context.CancellationToken);
    }
}
