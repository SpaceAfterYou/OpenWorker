using System.Diagnostics;
using Arch.Core;
using OpenWorker.Batch.Extensions;
using OpenWorker.Channel;
using OpenWorker.Domain.Components;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Enums;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Gameplay;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot.Modules.Maze.Requests;
using OpenWorker.Hotspot.Modules.Maze.Responses;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Lua;
using Redis.OM.Searching;

namespace OpenWorker.MazeServer.Services;

public sealed class MazeService(
    World ecs,
    BatchManager batches,
    IRedisCollection<DistrictReserveCache> districtReserves,
    WorldManager worldManager,
    IConfiguration configuration
) :
    IHotspotHandler<MazeExitRequest>,
    IHotspotHandler<MazeEventSpawnBoxRequest>,
    IHotspotHandler<MazeOperationEndRequest>,
    IHotspotHandler<MazeLuaFunctionRequest>
{
    private short Gate { get; } = configuration.GetGate();

    public async ValueTask OnHandleAsync(ServiceHandleContext context, MazeExitRequest request)
    {
        var claims = ecs.Get<ClaimsComponent>(context.Player);
        var world = ecs.Get<WorldComponent>(context.Player);

        if (!batches.TryGetAndCache(world.Location, out var mazeBatch))
        {
            return;
        }

        // var escape = mazeBatch.EventBox.MazeEscapes.First(x => x.Id == /* id of escape box for back to district */ 1001);
        var escape = mazeBatch.EventBox.MazeEscapes[0];

        var (districtCache, channelCache) = await worldManager
            .GetDistrictAsync(escape.Field)
            .ConfigureAwait(false);

        Debug.Assert(districtCache.Location == escape.Field);

        if (!batches.TryGetAndCache(districtCache.Location, out var districtBatch))
        {
            return;
        }

        var exit = districtBatch.EventBox.PortalExits.First(x => x.Id == escape.EventObject);

        var map = new MapValue
        {
            Location = districtCache.Location,
            Channel = channelCache.Identifier,
            Server = Gate
        };

        var location = new WorldValue
        {
            Location = districtCache.Location,
            Position = exit.GetRandomPosition(),
            Rotation = exit.Rotation,
            Map = map
        };

        var enter = new EnterMapResultValue
        {
            Zone = new ZoneValue
            {
                Account = claims,
                World = location,
                Jump = exit.Id,
                Address = districtCache.Host,
                Port = districtCache.Port,
                Type = EnterMapType.EnterDistrict,
                Map = map
            },
            ChangeServer = true,
            ChangeType = ChangeServerType.EnterDistrict
        };

        var position = exit.GetRandomPosition();

        var cache = new DistrictReserveCache
        {
            Account = claims,
            Person = ecs.Get<ActorComponent>(context.Player),
            District = districtCache.Guid,
            Channel = channelCache.Guid,
            Jump = exit.Id,
            X = position.X,
            Y = position.Y,
            Z = position.Z,
            R = exit.Rotation
        };

        await districtReserves
            .InsertAsync(cache)
            .ConfigureAwait(false);

        var session = ecs.Get<ServerSessionComponent>(context.Player);

        session.Send(new WorldEnterResponse { Map = enter });
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MazeEventSpawnBoxRequest request)
    {
        var maze = ecs.Get<LuaMaze>(context.Player);

        var boxes = maze.Batch.EventBox.CheckMonsterSpawns
            .First(x => x.Id == request.Box);

        foreach (var box in boxes.CheckBoxList.Where(x => x is not 0))
        {
            maze.ExecuteEventSpawnLua(box);
        }

        return ValueTask.CompletedTask;
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, MazeOperationEndRequest request)
    {
        var maze = ecs.Get<LuaMaze>(context.Player);

        await maze.State
            .CheckConditionAsync(0, request.Operation, maze)
            .ConfigureAwait(false);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, MazeLuaFunctionRequest request)
    {
        var session = ecs.Get<ServerSessionComponent>(context.Player);
        var world = ecs.Get<WorldComponent>(context.Player);

        if (!batches.TryGetAndCache(world.Location, out var batch, BatchType.Maze))
        {
            return;
        }

        var function = batch.EventBox.LuaFunctions.First(x => x.Id == request.Box);

        var state = ecs.Get<LuaMaze>(context.Player);

        await state.State
            .OnExecuteLuaFunction(function.Function, request.Box, state)
            .ConfigureAwait(false);

        session.Send(new MazeLuaFunctionResponse { Box = request.Box });
    }
}
