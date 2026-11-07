using OpenWorker.Gameplay.Modules.Vaccum;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Vaccum.Requests;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// Cubes are taken in mazes. Which cubes there are is told to <see cref="VaccumManager"/> by the maze.
/// </summary>
[HotspotHandler(HotspotHandlerType.Maze)]
public sealed class VaccumService(VaccumManager manager) :
    IHotspotHandler<VaccumClickStartRequest>,
    IHotspotHandler<VaccumClickCancelRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, VaccumClickStartRequest request)
    {
        manager.ClickStart(context.Player, request.Cube);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, VaccumClickCancelRequest request)
    {
        manager.ClickCancel(context.Player, request.Cube);

        return ValueTask.CompletedTask;
    }
}
