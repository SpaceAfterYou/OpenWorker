using Arch.Core;
using OpenWorker.Channel;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.World.Requests;

namespace OpenWorker.DistrictServer.Services;

public sealed class WorldService(World world, WorldManager manager) : IHotspotHandler<MazeCreateRequest>
{
    public async ValueTask OnHandleAsync(ServiceHandleContext context, MazeCreateRequest request)
    {
        await manager
            .TryEnterMaze(context.Player, request.Location)
            .ConfigureAwait(false);
    }
}