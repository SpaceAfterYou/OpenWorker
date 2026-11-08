using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using OpenWorker.Batch.Entities;
using Microsoft.Extensions.Options;
using OpenWorker.Batch.Entities.Basic;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Hotspot.Modules.Skill.Requests;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Hotspot.Modules.World.Enums;
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
using OpenWorker.Hotspot.Modules.World.Requests;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Lua;
using Redis.OM.Searching;

namespace OpenWorker.MazeServer.Services;

public sealed partial class MazeService(
    World ecs,
    BatchManager batches,
    IRedisCollection<DistrictReserveCache> districtReserves,
    WorldManager worldManager,
    IOptions<GateOptions> gate,
    QuestManager questManager,
    ILogger<MazeService> logger
) :
    IHotspotHandler<MazeCompleteRequest>,
    IHotspotHandler<SkillActiveSkillRequest>,
    IHotspotHandler<SkillPassiveRequest>,
    IHotspotHandler<SkillPassiveEndRequest>,
    IHotspotHandler<SkillSyncPositionRequest>,
    IHotspotHandler<SkillProjectileRequest>,
    IHotspotHandler<SkillWarpPositionRequest>,
    IHotspotHandler<WorldWarpRequest>,
    IHotspotHandler<WorldWarpOutRequest>,
    IHotspotHandler<WorldCheckSectorRequest>,
    IHotspotHandler<WorldEscapeRequest>,
    IHotspotHandler<MazeSceneDirectingRequest>,
    IHotspotHandler<MazeCheckEventSpawnBoxRequest>,
    IHotspotHandler<MazeQuestMoveCheckRequest>,
    IHotspotHandler<MazeInteractionClickRequest>,
    IHotspotHandler<MazeExitRequest>,
    IHotspotHandler<MazeEventSpawnBoxRequest>,
    IHotspotHandler<MazeOperationEndRequest>,
    IHotspotHandler<MazeLuaFunctionRequest>,
    IHotspotHandler<WorldEnterRequest>
{
    private const ushort WarpFailed = 55014;
    private const ushort SectorWrongMap = 55005;
    private const ushort SectorNotFound = 55006;
    private const ushort InteractionFailed = 54007;

    /// <summary>
    /// Keeps whatever motion the character is in.
    /// </summary>
    private const short NoMotion = -1;

    private short Gate { get; } = gate.Value.Id;

    public async ValueTask OnHandleAsync(ServiceHandleContext context, WorldEnterRequest request)
    {
        await worldManager
            .EnterAsync(context.Player, request.Location, request.Jump);
    }

    /// <summary>
    /// The client has finished loading the maze, so what the script does now reaches it.
    /// </summary>
    public async ValueTask OnHandleAsync(ServiceHandleContext context, MazeCompleteRequest request)
    {
        if (!ecs.Has<LuaMaze>(context.Player))
        {
            return;
        }

        var maze = ecs.Get<LuaMaze>(context.Player);

        if (!maze.TryStart())
        {
            return;
        }

        SendProgress(context.Player, maze);

        if (FindSector(maze, ecs.Get<WorldComponent>(context.Player).Position) is { } sector)
        {
            maze.SpawnSector(sector.Id);
        }

        await maze.State.OnEnterPlayerAsync(ecs.Get<ActorComponent>(context.Player), maze);
    }

    private void SendProgress(Entity player, LuaMaze maze)
    {
        var session = ecs.Get<ServerSessionComponent>(player);
        var progress = maze.Progress;

        session.Send(new MazeSectorInfosResponse
        {
            Cleared = progress.Cleared,
            Current = FindSector(maze, ecs.Get<WorldComponent>(player).Position)?.Id ?? 0
        });
        session.Send(new MazeGateInfosResponse { ReEnter = false, Open = progress.OpenGates });
        session.Send(new MazeDieMonstersResponse { SpawnBoxes = [] });
        session.Send(new MazePortalInfosResponse { Open = progress.OpenPortals, Closed = progress.ClosedPortals });
        session.Send(new MazeInteractionInfosResponse { Interactions = progress.InteractionList });
    }

    private static SectorBox? FindSector(LuaMaze maze, Vector3 position)
    {
        return maze.Batch.EventBox.Sectors.FirstOrDefault(e =>
            position.X >= Math.Min(e.PosTopLeft.X, e.PosBottomRight.X) &&
            position.X <= Math.Max(e.PosTopLeft.X, e.PosBottomRight.X) &&
            position.Y >= Math.Min(e.PosTopLeft.Y, e.PosBottomRight.Y) &&
            position.Y <= Math.Max(e.PosTopLeft.Y, e.PosBottomRight.Y));
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillActiveSkillRequest request)
    {
        var skill = request.Skill;
        var world = ecs.Get<WorldComponent>(context.Player);

        if (Place.IsValid(skill.Position.Position, skill.Position.Angle))
        {
            ecs.Set(context.Player, world with { Position = skill.Position.Position, Rotation = skill.Position.Angle });
        }

        ecs.Get<ServerSessionComponent>(context.Player).Send(new SkillActiveResponse { Divergence = skill.Divergence });

        if (ecs.Has<LuaMaze>(context.Player))
        {
            ecs.Get<LuaMaze>(context.Player).StartSkill(skill.Skill, skill.Position);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Skills that move the player report the place along the way, and the next attack starts from there.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillSyncPositionRequest request)
    {
        if (Place.IsValid(request.Position, request.Yaw))
        {
            ecs.Set(context.Player, ecs.Get<WorldComponent>(context.Player) with { Position = request.Position, Rotation = request.Yaw });
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// A skill that jumps the player somewhere. The new place goes back to the client.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillWarpPositionRequest request)
    {
        if (!Place.IsValid(request.Position))
        {
            return ValueTask.CompletedTask;
        }

        var place = ecs.Get<WorldComponent>(context.Player) with { Position = request.Position };

        ecs.Set(context.Player, place);
        ecs.Get<ServerSessionComponent>(context.Player).Send(new SkillWarpPositionBroadcastResponse
        {
            Actor = ecs.Get<ActorComponent>(context.Player),
            PosInfo = new SkillPositionValue
            {
                Position = place.Position,
                Angle = place.Rotation,
                MotionClass = NoMotion
            }
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillPassiveRequest request)
    {
        ecs.Get<ServerSessionComponent>(context.Player).Send(new SkillPassiveResponse());

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillPassiveEndRequest request)
    {
        var place = request.Skill.Position;

        if (Place.IsValid(place.Position, place.Angle))
        {
            ecs.Set(context.Player, ecs.Get<WorldComponent>(context.Player) with { Position = place.Position, Rotation = place.Angle });
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillProjectileRequest request)
    {
        if (Place.IsValid(request.Position, request.Yaw) &&
            Place.IsValid(request.Right) &&
            Place.IsValid(request.Direction) &&
            ecs.Has<LuaMaze>(context.Player))
        {
            ecs.Get<LuaMaze>(context.Player).FireProjectile(request);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// A portal that leads to another place on this map.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, WorldWarpRequest request)
    {
        if (!ecs.Has<LuaMaze>(context.Player))
        {
            return ValueTask.CompletedTask;
        }

        var maze = ecs.Get<LuaMaze>(context.Player);
        var world = ecs.Get<WorldComponent>(context.Player);
        var events = maze.Batch.EventBox;

        BasicEntity? destination = events.PortalExits.FirstOrDefault(e => e.Id == request.Jump);
        destination ??= events.SectorStarts.FirstOrDefault(e => e.Id == request.Jump);
        destination ??= events.StartEvents.FirstOrDefault(e => e.Id == request.Jump);

        if (request.Location != world.Location || !maze.Progress.IsPortalOpen(request.Portal) || destination is null)
        {
            SendError(context.Player, WorldOpcode.WarpReq, WarpFailed);

            return ValueTask.CompletedTask;
        }

        Warp(context.Player, destination.GetRandomPosition(), destination.Rotation);

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Stepping out of a portal that waits for the whole party. Nothing waits while every player has a
    /// maze of their own.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, WorldWarpOutRequest request)
    {
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// The client reached a box that opens a gate when its sector is cleared.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, WorldCheckSectorRequest request)
    {
        if (!ecs.Has<LuaMaze>(context.Player))
        {
            SendError(context.Player, WorldOpcode.CheckSectorReq, SectorNotFound);

            return ValueTask.CompletedTask;
        }

        var maze = ecs.Get<LuaMaze>(context.Player);
        var box = maze.Batch.EventBox.CheckSectors.FirstOrDefault(e => e.Id == request.Box);

        if (box is null)
        {
            SendError(context.Player, WorldOpcode.CheckSectorReq, SectorNotFound);
        }
        else if (request.Location != ecs.Get<WorldComponent>(context.Player).Location)
        {
            SendError(context.Player, WorldOpcode.CheckSectorReq, SectorWrongMap);
        }
        else
        {
            maze.SetGateFlag(box.CheckGate, maze.Progress.IsSectorCleared(box.CheckSector));
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// A player who got stuck goes back to the start of the current sector.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, WorldEscapeRequest request)
    {
        if (!ecs.Has<LuaMaze>(context.Player))
        {
            return ValueTask.CompletedTask;
        }

        var maze = ecs.Get<LuaMaze>(context.Player);
        var world = ecs.Get<WorldComponent>(context.Player);
        var events = maze.Batch.EventBox;

        var sector = FindSector(maze, world.Position);

        BasicEntity? start = sector is null ? null : events.SectorStarts.FirstOrDefault(e => e.Sector == sector.Id);
        start ??= events.StartEvents.FirstOrDefault(e => e.Id == world.Jump);

        if (start is not null)
        {
            Warp(context.Player, start.GetRandomPosition(), start.Rotation);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MazeSceneDirectingRequest request)
    {
        ecs.Get<ServerSessionComponent>(context.Player).Send(new MazeSceneDirectingResponse
        {
            Box = request.Box,
            Actor = request.Actor
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MazeCheckEventSpawnBoxRequest request)
    {
        if (!ecs.Has<LuaMaze>(context.Player))
        {
            return ValueTask.CompletedTask;
        }

        var maze = ecs.Get<LuaMaze>(context.Player);
        var box = maze.Batch.EventBox.CheckEventSpawns.FirstOrDefault(e => e.Id == request.Box);

        if (box is not null && maze.Progress.TryUseBox(box.Id))
        {
            foreach (var spawn in box.SpawnBoxList.Where(e => e != 0))
            {
                maze.ExecuteEventSpawnLua(spawn);
            }
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MazeQuestMoveCheckRequest request)
    {
        if (!ecs.Has<LuaMaze>(context.Player))
        {
            return ValueTask.CompletedTask;
        }

        var maze = ecs.Get<LuaMaze>(context.Player);
        var box = maze.Batch.EventBox.QuestMoveChecks.FirstOrDefault(e => e.Id == request.Box);

        if (box is not null && maze.Progress.TryUseBox(box.Id))
        {
            questManager.TryApplyConditionProgress(context.Player, box.EpisodeCondition, 1);

            ecs.Get<ServerSessionComponent>(context.Player).Send(new MazeQuestMoveCheckResponse { Box = box.Id });
        }

        return ValueTask.CompletedTask;
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, MazeInteractionClickRequest request)
    {
        if (!ecs.Has<LuaMaze>(context.Player))
        {
            return;
        }

        var maze = ecs.Get<LuaMaze>(context.Player);

        if (!maze.Progress.TryGetInteraction(request.Box, out var interaction) || !interaction.Enable)
        {
            SendError(context.Player, MazeOpcode.eSUB_CMD_MAZE_INTERACTION_CLICK_REQ, InteractionFailed);
            return;
        }

        ecs.Get<ServerSessionComponent>(context.Player).Send(new MazeInteractionRunResponse
        {
            Result = 0,
            Interaction = maze.Progress.UseInteraction(interaction)
        });

        await maze.State.OnInteractionObject(request.Box, maze);
    }

    private void Warp(Entity player, Vector3 position, float rotation)
    {
        var world = ecs.Get<WorldComponent>(player);

        ecs.Set(player, world with { Position = position, Rotation = rotation });

        ecs.Get<ServerSessionComponent>(player).Send(new WorldWarpResponse
        {
            Actor = ecs.Get<ActorComponent>(player),
            Position = position,
            Rotation = rotation,
            Result = WorldWrapResult.Ok
        });
    }

    private void SendError(Entity player, WorldOpcode command, ushort code)
    {
        ecs.Get<ServerSessionComponent>(player).Send(new ErrorMessageResponse(new MessageOpcode(GroupOpcode.World, command), code));
    }

    private void SendError(Entity player, MazeOpcode command, ushort code)
    {
        ecs.Get<ServerSessionComponent>(player).Send(new ErrorMessageResponse(new MessageOpcode(GroupOpcode.Maze, command), code));
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, MazeExitRequest request)
    {
        var claims = ecs.Get<ClaimsComponent>(context.Player);
        var world = ecs.Get<WorldComponent>(context.Player);

        var session = ecs.Get<ServerSessionComponent>(context.Player);

        if (!batches.TryGetAndCache(world.Location, out var mazeBatch) ||
            mazeBatch.EventBox.MazeEscapes.FirstOrDefault() is not { } escape)
        {
            session.Send(new WorldEnterResponse { Map = EnterMapResultValue.Error });
            return;
        }

        var placement = await worldManager.GetDistrictAsync(escape.Field);

        // The player may have left while the servers were looked up.
        if (!ecs.IsAlive(context.Player))
        {
            return;
        }

        if (placement is null)
        {
            LogNoDistrictToExit(world.Location, escape.Field);

            session.Send(new WorldEnterResponse { Map = EnterMapResultValue.Failed(EnterMapResultValue.ServerUnavailable) });
            return;
        }

        var (districtCache, channelCache) = placement;

        if (!batches.TryGetAndCache(districtCache.Location, out var districtBatch))
        {
            session.Send(new WorldEnterResponse { Map = EnterMapResultValue.Error });
            return;
        }

        // A map leads either to a portal exit or to a start point of the district.
        BasicEntity? exit = districtBatch.EventBox.PortalExits.FirstOrDefault(x => x.Id == escape.EventObject);
        exit ??= districtBatch.EventBox.StartEvents.FirstOrDefault(x => x.Id == escape.EventObject);
        exit ??= districtBatch.EventBox.StartEvents.FirstOrDefault(x => x.Id == districtCache.Location * 100 + 1);

        if (exit is null)
        {
            session.Send(new WorldEnterResponse { Map = EnterMapResultValue.Error });
            return;
        }

        var map = new MapValue
        {
            Location = districtCache.Location,
            Channel = channelCache.Identifier,
            Server = Gate
        };

        var position = exit.GetRandomPosition();

        var location = new WorldValue
        {
            Location = districtCache.Location,
            Position = position,
            Rotation = exit.Rotation,
            Map = map
        };

        var enter = new EnterMapResultValue
        {
            Zone = new ZoneValue
            {
                Actor = ecs.Get<ActorComponent>(context.Player).Identifier,
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

        var cache = new DistrictReserveCache
        {
            Account = claims,
            Person = ecs.Get<ActorComponent>(context.Player).Identifier,
            District = districtCache.Guid,
            Channel = channelCache.Guid,
            Jump = exit.Id,
            X = position.X,
            Y = position.Y,
            Z = position.Z,
            R = exit.Rotation
        };

        await districtReserves
            .InsertAsync(cache, WorldManager.ReservationLifetime);

        session.Send(new WorldEnterResponse { Map = enter });
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MazeEventSpawnBoxRequest request)
    {
        if (!TryGetMaze(context.Player, nameof(MazeEventSpawnBoxRequest), out var maze))
        {
            return ValueTask.CompletedTask;
        }

        // The box id comes from the client, which can name a box that this map does not have.
        if (maze.Batch.EventBox.CheckMonsterSpawns.FirstOrDefault(x => x.Id == request.Box) is not { } boxes)
        {
            LogUnknownBox(nameof(MazeEventSpawnBoxRequest), request.Box);

            return ValueTask.CompletedTask;
        }

        foreach (var box in boxes.CheckBoxList.Where(x => x is not 0))
        {
            maze.ExecuteEventSpawnLua(box);
        }

        return ValueTask.CompletedTask;
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, MazeOperationEndRequest request)
    {
        if (!TryGetMaze(context.Player, nameof(MazeOperationEndRequest), out var maze))
        {
            return;
        }

        await maze.State
            .CheckConditionAsync(0, request.Operation, maze);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, MazeLuaFunctionRequest request)
    {
        if (!TryGetMaze(context.Player, nameof(MazeLuaFunctionRequest), out var maze))
        {
            return;
        }

        // The box id comes from the client, which can name a box that this map does not have.
        if (maze.Batch.EventBox.LuaFunctions.FirstOrDefault(x => x.Id == request.Box) is not { } function)
        {
            LogUnknownBox(nameof(MazeLuaFunctionRequest), request.Box);

            return;
        }

        await maze.State
            .OnExecuteLuaFunction(function.Function, request.Box, maze);

        // The script may have taken a while, and the player may have left meanwhile.
        if (ecs.IsAlive(context.Player))
        {
            ecs.Get<ServerSessionComponent>(context.Player).Send(new MazeLuaFunctionResponse { Box = request.Box });
        }
    }

    /// <summary>
    /// A player who has not entered a maze has none, and what they ask of it is ignored.
    /// </summary>
    private bool TryGetMaze(Entity player, string request, [NotNullWhen(true)] out LuaMaze? maze)
    {
        if (ecs.Has<LuaMaze>(player))
        {
            maze = ecs.Get<LuaMaze>(player);

            return true;
        }

        LogNotInMaze(request);

        maze = null;

        return false;
    }

    // What a client sends is no reason to fill the log at a higher level than Debug.
    [LoggerMessage(Level = LogLevel.Debug, Message = "{Request} from a player who is not in a maze, ignored")]
    private partial void LogNotInMaze(string request);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{Request} names box {Box}, which the maze does not have, ignored")]
    private partial void LogUnknownBox(string request, int box);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The maze {Maze} leads to location {Location}, but no district of it can take a player")]
    private partial void LogNoDistrictToExit(int maze, int location);
}
