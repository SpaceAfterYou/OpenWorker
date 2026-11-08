using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using OpenWorker.Batch;
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
using OpenWorker.Hotspot.Modules.Persons.Requests;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Hotspot.Modules.World.Requests;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Hotspot.Modules.World.Types;
using OpenWorker.Domain.Batch.Enums;
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
    IHotspotHandler<SkillChainRequest>,
    IHotspotHandler<SkillKeypressRequest>,
    IHotspotHandler<SkillTrapPosUpdateRequest>,
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
    IHotspotHandler<MazeSwitchNpcClickRequest>,
    IHotspotHandler<MazeInteractionMotionRequest>,
    IHotspotHandler<MazeMonsterClientSpawnRequest>,
    IHotspotHandler<MazeEnterPartyAnswerRequest>,
    IHotspotHandler<PersonUpdateCutsceneRequest>,
    IHotspotHandler<WorldEnterRequest>
{
    private const ushort WarpFailed = 55014;
    private const ushort SectorWrongMap = 55005;
    private const ushort SectorNotFound = 55006;
    private const ushort InteractionFailed = 54007;

    /// <summary>
    /// The answer to a request that names a box the map of the maze does not have: the code the world gives for the box
    /// of a sector it cannot find.
    /// </summary>
    private const ushort BoxNotFound = SectorNotFound;

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

        maze.HideUnfoundSoulObjects();

        SendProgress(context.Player, maze);

        maze.RegisterCubes();

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
    /// The player holds the key of the skill or lets it go. Nobody else shares this maze to be told about it.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillKeypressRequest request)
    {
        if (!ecs.Has<LuaMaze>(context.Player))
        {
            return ValueTask.CompletedTask;
        }

        if (Place.IsValid(Vector3.Zero, request.Rotation))
        {
            ecs.Set(context.Player, ecs.Get<WorldComponent>(context.Player) with { Rotation = request.Rotation });
        }

        ecs.Get<LuaMaze>(context.Player).PressKey(request.IsKeyPressed);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillChainRequest request)
    {
        var chain = request.Projectile;

        if (Place.IsValid(chain.Position) && Place.IsValid(chain.Direction) && ecs.Has<LuaMaze>(context.Player))
        {
            ecs.Get<LuaMaze>(context.Player).FireChain(chain);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// The player moves a trap with the mouse. The trap follows, and nobody else in the maze is told.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillTrapPosUpdateRequest request)
    {
        if (Place.IsValid(request.Position) && ecs.Has<LuaMaze>(context.Player))
        {
            ecs.Get<LuaMaze>(context.Player).MoveTrap(request.Position);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// A portal that leads to another place on this map. It shuts behind the player, and what lies beyond is
    /// already alive: the portal woke it when it opened.
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

        BasicEntity? destination = events.PortalExits.Find(request.Jump);
        destination ??= events.SectorStarts.Find(request.Jump);
        destination ??= events.StartEvents.Find(request.Jump);

        // The portal of a boss sector is never shut: whoever fights the boss must be able to leave.
        var inBossSector = FindSector(maze, world.Position)?.Type == SectorType.Boss;

        if (request.Location != world.Location || !(inBossSector || maze.Progress.IsPortalOpen(request.Portal)) || destination is null)
        {
            SendError(context.Player, WorldOpcode.WarpReq, WarpFailed);

            return ValueTask.CompletedTask;
        }

        var position = destination.GetRandomPosition();

        ecs.Set(context.Player, world with { Position = position, Rotation = destination.Rotation });

        ecs.Get<ServerSessionComponent>(context.Player).Send(new WorldWarpMazeResponse
        {
            BoxIndex = request.Portal,
            Warp = new WarpValue { HasError = false, Position = position, Rotation = destination.Rotation },
            // The map says which sector the portal leads to, and sectors may lie inside one another.
            Sector = events.Portals.Find(request.Portal) is { NextSector: > 0 } portal ? portal.NextSector : FindSector(maze, position)?.Id ?? 0
        });

        maze.SetPotalFlag(request.Portal, false);

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
        var box = maze.Batch.EventBox.CheckSectors.Find(request.Box);

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

        BasicEntity? start = sector is null ? null : events.StartOf(sector.Id);
        start ??= events.StartEvents.Find(world.Jump);

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
        // The box id comes from the client, which can name a box that this map does not have.
        if (maze.Batch.EventBox.CheckEventSpawns.Find(request.Box) is not { } box)
        {
            RefuseUnknownBox(context.Player, MazeOpcode.eSUB_CMD_CHECK_EVENT_SPAWN_BOX_REQ, nameof(MazeCheckEventSpawnBoxRequest), request.Box);

            return ValueTask.CompletedTask;
        }

        if (maze.Progress.TryUseBox(box.Id))
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
        if (maze.Batch.EventBox.QuestMoveChecks.Find(request.Box) is not { } box)
        {
            RefuseUnknownBox(context.Player, MazeOpcode.eSUB_CMD_MAZE_QUEST_MOVE_CHECK_REQ, nameof(MazeQuestMoveCheckRequest), request.Box);

            return ValueTask.CompletedTask;
        }

        if (maze.Progress.TryUseBox(box.Id))
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

        await maze.CollectSoulObjectAsync(request.Box);

        await maze.State.OnInteractionObject(request.Box, maze);
    }

    /// <summary>
    /// Where a portal of the maze leads out of it, or the escape box when the client names no such portal.
    /// </summary>
    private static (int Field, int EventObject)? FindWayOut(VBatchFile batch, int portal)
    {
        if (batch.EventBox.Portals.Find(portal) is { JumpType: JumpType.Field } leaving)
        {
            return (leaving.JumpMap, leaving.Jump);
        }

        return batch.EventBox.MazeEscapes is [var escape, ..] ? (escape.Field, escape.EventObject) : null;
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

    public ValueTask OnHandleAsync(ServiceHandleContext context, MazeSwitchNpcClickRequest request)
    {
        if (ecs.Has<LuaMaze>(context.Player))
        {
            ecs.Get<LuaMaze>(context.Player).ClickSwitchNpc(request.Npc);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// The motion of using an object is shown to the others. Nobody shares this maze, and the client
    /// asks to use the object itself when the motion is over.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, MazeInteractionMotionRequest request)
    {
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Monsters that a client controls and that summon others are not here: the server keeps every monster
    /// of this maze itself.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, MazeMonsterClientSpawnRequest request)
    {
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Nobody calls a player into a maze of one.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, MazeEnterPartyAnswerRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PersonUpdateCutsceneRequest request)
    {
        if (ecs.Has<LuaMaze>(context.Player))
        {
            await ecs.Get<LuaMaze>(context.Player).UpdateCutsceneAsync(request.Play, request.Name);
        }
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, MazeExitRequest request)
    {
        var claims = ecs.Get<ClaimsComponent>(context.Player);
        var world = ecs.Get<WorldComponent>(context.Player);

        var session = ecs.Get<ServerSessionComponent>(context.Player);

        // The player leaves through the portal of the maze that leads out of it, or through the escape box.
        if (!batches.TryGetAndCache(world.Location, out var mazeBatch) ||
            FindWayOut(mazeBatch, request.Portal) is not var (field, eventObject))
        {
            session.Send(new WorldEnterResponse { Map = EnterMapResultValue.Error });
            return;
        }

        var placement = await worldManager.GetDistrictAsync(field);

        // The player may have left while the servers were looked up.
        if (!ecs.IsAlive(context.Player))
        {
            return;
        }

        if (placement is null)
        {
            LogNoDistrictToExit(world.Location, field);

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
        BasicEntity? exit = districtBatch.EventBox.PortalExits.Find(eventObject);
        exit ??= districtBatch.EventBox.StartEvents.Find(eventObject);
        exit ??= districtBatch.EventBox.StartEvents.Find(districtCache.Location * 100 + 1);

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
        if (maze.Batch.EventBox.CheckMonsterSpawns.Find(request.Box) is not { } boxes)
        {
            RefuseUnknownBox(context.Player, MazeOpcode.eSUB_CMD_EVENT_SPAWN_BOX_REQ, nameof(MazeEventSpawnBoxRequest), request.Box);

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
        if (maze.Batch.EventBox.LuaFunctions.Find(request.Box) is not { } function)
        {
            RefuseUnknownBox(context.Player, MazeOpcode.eSUB_CMD_MAZE_LUA_FUNCTION_REQ, nameof(MazeLuaFunctionRequest), request.Box);

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
    /// A request about a box the map does not have gets the error answer, and nothing else happens.
    /// </summary>
    private void RefuseUnknownBox(Entity player, MazeOpcode command, string request, int box)
    {
        LogUnknownBox(request, box);

        SendError(player, command, BoxNotFound);
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

    [LoggerMessage(Level = LogLevel.Debug, Message = "{Request} names box {Box}, which the maze does not have, refused")]
    private partial void LogUnknownBox(string request, int box);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The maze {Maze} leads to location {Location}, but no district of it can take a player")]
    private partial void LogNoDistrictToExit(int maze, int location);
}
