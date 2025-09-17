using Arch.Core;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Maze.Requests;
using OpenWorker.Hotspot.Modules.Maze.Responses;

namespace OpenWorker.DistrictServer.Services;

public sealed class MazeService(World world) : IHotspotHandler<MazeLuaFunctionRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, MazeLuaFunctionRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);
        
        session.Send(new MazeLuaFunctionResponse(request.Box));

        return ValueTask.CompletedTask;
    }
}