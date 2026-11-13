using OpenWorker.Gameplay.Modules.Attendance;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Events.Requests;

namespace OpenWorker.DistrictServer.Services;

/// <summary>
/// The reward of a day of attendance goes to the person by itself, so its request only makes the server look again; the
/// request of play time claims the step the person has reached.
/// </summary>
public sealed class EventService(AttendanceManager attendance) :
    IHotspotHandler<EventAttendancePlayTimeRewardRequest>,
    IHotspotHandler<EventAttendanceRewardRequest>
{
    public async ValueTask OnHandleAsync(ServiceHandleContext context, EventAttendancePlayTimeRewardRequest request)
    {
        await attendance.ClaimPlayTimeAsync(context.Player, context.CancellationToken);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, EventAttendanceRewardRequest request)
    {
        await attendance.RefreshAsync(context.Player, context.CancellationToken);
    }
}
