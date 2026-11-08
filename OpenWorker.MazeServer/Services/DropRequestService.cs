using OpenWorker.Gameplay.Modules.Drops;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Drops;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// The client asks to take a thing it has seen on the ground.
/// </summary>
[HotspotHandler(HotspotHandlerType.District)]
public sealed class DropRequestService(DropService drops) : IHotspotHandler<DropPickUpRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, DropPickUpRequest request)
    {
        drops.PickUp(context.Player, request.Serial);

        return ValueTask.CompletedTask;
    }
}
