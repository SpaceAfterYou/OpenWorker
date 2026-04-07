using OpenWorker.Channel;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Gameplay;
using OpenWorker.Hotspot.Modules.Channels;
using OpenWorker.Hotspot.Modules.Movement.Requests;

namespace OpenWorker.DistrictServer.Services;

[HotspotHandler(HotspotHandlerType.District)]
public sealed class MovementService(ServiceChannels channels) :
    IHotspotHandler<MovementMoveRequest>,
    IHotspotHandler<MovementStopRequest>,
    IHotspotHandler<MovementJumpRequest>,
    IHotspotHandler<MovementBattleRequest>,
    IHotspotHandler<MovementMotionRequest>,
    IHotspotHandler<MovementIgnoreMotionDeltaRequest>,
    IHotspotHandler<MovementTransportTakeRequest>,
    IHotspotHandler<MovementTransportOffRequest>,
    IHotspotHandler<MovementRotationRequest>,
    IHotspotHandler<MovementPositionRequest>,
    IHotspotHandler<MovementLoopMotionStartRequest>,
    IHotspotHandler<MovementLoopMotionEndRequest>,
    IHotspotHandler<MovementAttacedEndRequest>,
    IHotspotHandler<MovementGroundStatusRequest>,
    IHotspotHandler<MovementJumpQuickDownRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementAttacedEndRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementBattleRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementGroundStatusRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementIgnoreMotionDeltaRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementJumpQuickDownRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementJumpRequest request)
    {
        var channel = channels.Get(context.Player);

        channel.ForEach(e =>
        {
            // TODO: broadcast movement
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementLoopMotionEndRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementLoopMotionStartRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementMotionRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementMoveRequest request)
    {
        var channel = channels.Get(context.Player);

        channel.ForEach(e =>
        {
            // TODO: broadcast movement
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementPositionRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementRotationRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementStopRequest request)
    {
        var channel = channels.Get(context.Player);

        channel.ForEach(e =>
        {
            // TODO: broadcast movement
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementTransportOffRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementTransportTakeRequest request)
    {
        return ValueTask.CompletedTask;
    }
}
