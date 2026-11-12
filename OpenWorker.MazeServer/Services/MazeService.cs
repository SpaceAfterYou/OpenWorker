using System.Diagnostics.CodeAnalysis;
using OpenWorker.Gameplay.Mapping;
using System.Numerics;
using OpenWorker.Batch;
using OpenWorker.Batch.Entities;
using Microsoft.Extensions.Options;
using OpenWorker.Batch.Entities.Basic;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Maze;
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
using OpenWorker.Gameplay.Combat;
using OpenWorker.Server.Common;
using OpenWorker.Server.Common.Services;
using OpenWorker.Hotspot.Handler;
using Redis.OM.Searching;
using OpenWorker.Gameplay.Modules.Persistence;

namespace OpenWorker.MazeServer.Services;

public sealed partial class MazeService(
    World ecs,
    BatchManager batches,
    IRedisCollection<DistrictReserveCache> districtReserves,
    WorldManager worldManager,
    IPersonFlush persons,
    IOptions<GateOptions> gate,
    QuestManager questManager,
    PrologueService prologue,
    MazeInstances instances,
    SkillUse skillUse,
    MovementPace pace,
    HandlerExecutor errors,
    SimulationClock clock,
    IPlayerAudience audience,
    SkillInputRelay relay,
    MazeAdmission admission,
    ILogger<MazeService> logger,
    MazeHelperField? helpers = null
) :
    IHotspotHandler<MazeCompleteRequest>,
    IHotspotHandler<SkillActiveSkillRequest>,
    IHotspotHandler<SkillPassiveRequest>,
    IHotspotHandler<SkillPassiveEndRequest>,
    IHotspotHandler<SkillSyncPositionRequest>,
    IHotspotHandler<SkillProjectileRequest>,
    IHotspotHandler<SkillChainRequest>,
    IHotspotHandler<SkillKeypressRequest>,
    IHotspotHandler<SkillTargetChangeRequest>,
    IHotspotHandler<SkillSubInputRequest>,
    IHotspotHandler<SkillChargingRequest>,
    IHotspotHandler<SkillPreTargetListRequest>,
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

    /// <summary>
    /// The player takes a portal of the maze's map into a district or another maze, or the tutorial moves it on to its second
    /// maze. The way out asks what the portal of a district asks (<see cref="MazeAdmission"/>): the player stands at a portal
    /// of this map, and a maze asks its level, the maze before it, its daily limit, its fatigue and its item; the tutorial
    /// mazes are entered from wherever the story puts the player. A refusal is the error of the request with the code.
    /// </summary>
    public async ValueTask OnHandleAsync(ServiceHandleContext context, WorldEnterRequest request)
    {
        if (admission.Check(context.Player, request.Location, request.Portal, out var item) is { } code)
        {
            ecs.Get<ServerSessionComponent>(context.Player)
                .Send(new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.World, WorldOpcode.EnterReq), code, item > 0 ? (uint)item : null));

            return;
        }

        await worldManager
            .EnterAsync(context.Player, request.Location, request.Jump);
    }

    /// <summary>
    /// The client has finished loading the maze, so what the script does now reaches it. The first player starts the maze;
    /// a member of the party who comes later finds it where it stands: what lives in it, the other members, and they the
    /// newcomer.
    /// </summary>
    public async ValueTask OnHandleAsync(ServiceHandleContext context, MazeCompleteRequest request)
    {
        if (!ecs.Has<LuaMaze>(context.Player))
        {
            return;
        }

        var maze = ecs.Get<LuaMaze>(context.Player);

        if (!maze.MarkLoaded(context.Player))
        {
            return;
        }

        var first = maze.TryStart();

        if (first)
        {
            maze.HideUnfoundSoulObjects(context.Player);
        }

        SendProgress(context.Player, maze);

        if (!first)
        {
            maze.Creatures.ShowAllTo(ecs.Get<ServerSessionComponent>(context.Player));
            maze.Helpers.ShowTo(ecs.Get<ServerSessionComponent>(context.Player));
        }

        ShowMembers(context.Player, maze);

        maze.RegisterCubes(context.Player);

        if (FindSector(maze, ecs.Get<WorldComponent>(context.Player).Position) is { } sector)
        {
            maze.SpawnSector(sector.Id);
        }

        // A player alone has its helpers called by themselves, if it asked for that.
        if (helpers is not null)
        {
            await helpers.AutoSummonAsync(context.Player);

            if (!ecs.IsAlive(context.Player) || maze.IsClosed)
            {
                return;
            }
        }

        await maze.State.OnEnterPlayerAsync(ecs.Get<ActorComponent>(context.Player), maze);
    }

    /// <summary>
    /// The player and the other members of the party in the maze see each other.
    /// </summary>
    private void ShowMembers(Entity player, LuaMaze maze)
    {
        if (maze.MemberCount < 2)
        {
            return;
        }

        var session = ecs.Get<ServerSessionComponent>(player);

        foreach (var other in maze.Members)
        {
            if (other != player && ecs.IsAlive(other))
            {
                session.Send(Arrival(other));
            }
        }

        maze.Creatures.BroadcastToOthers(player, Arrival(player));
    }

    private WorldInInfoPcResponse Arrival(Entity player)
    {
        return new WorldInInfoPcResponse
        {
            Person = PersonSnapshotMapper.CreatePersonValue(ecs, player, player),
            World = PersonSnapshotMapper.CreateWorldValue(ecs, player)
        };
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

    private static SectorBox? FindSector(LuaMaze maze, Vector3 position) => maze.SectorAt(position);

    /// <summary>
    /// The player starts a skill. It must be the player's own and in its deck, its cooldown over and its cost payable; a
    /// refusal is the answer 06/09 with the code the client shows, and nothing starts. The skill starts from the place the
    /// client gives when the player could be there, from the place the server keeps otherwise.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillActiveSkillRequest request)
    {
        var skill = request.Skill;
        var session = ecs.Get<ServerSessionComponent>(context.Player);
        var position = PlaceOfSkill(context.Player, skill.Position);
        var now = clock.Time;

        var refusal = skillUse.Check(context.Player, skill.Skill, now);

        if (refusal != SkillRefusal.None)
        {
            LogSkillRefused(skill.Skill, refusal);

            session.Send(new SkillActiveResponse { ErrorCode = unchecked((short)refusal), Divergence = skillUse.DivergenceOf(context.Player, skill.Skill, skill.ParentSkill) });

            return ValueTask.CompletedTask;
        }

        skillUse.Use(context.Player, skill.Skill, now);

        var divergence = skillUse.DivergenceOf(context.Player, skill.Skill, skill.ParentSkill);

        session.Send(new SkillActiveResponse { Divergence = divergence });

        // The other members of the party play the skill from the place the server took for it.
        audience.NearbyToOthers(context.Player, PlayerSkillBroadcast.Started(ecs, context.Player, skill, position, divergence));

        if (ecs.Has<LuaMaze>(context.Player))
        {
            ecs.Get<LuaMaze>(context.Player).StartSkill(context.Player, skill.Skill, position);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Where a skill starts: the place of the request when it is a place and the player could have run there since the last
    /// one, the place the server keeps otherwise (a refused place counts against the error budget, as a refused move does).
    /// </summary>
    private SkillPositionValue PlaceOfSkill(Entity player, SkillPositionValue requested)
    {
        var world = ecs.Get<WorldComponent>(player);
        var rotation = Place.IsValid(world.Position, requested.Angle) ? requested.Angle : world.Rotation;

        if (!Place.IsValid(requested.Position))
        {
            return requested with { Position = world.Position, Angle = rotation };
        }

        if (pace.Check(ecs, player, world.Position, requested.Position, out var charge) != MoveVerdict.Taken)
        {
            if (charge)
            {
                errors.Charge(player, MessageOpcode.From(GroupOpcode.Skill, SkillOpcode.ActiveSkillReq), HandlerErrorReason.Violation);
            }

            return requested with { Position = world.Position, Angle = rotation };
        }

        ecs.Set(player, world with { Position = requested.Position, Rotation = rotation });

        return requested with { Angle = rotation };
    }

    /// <summary>
    /// Skills that move the player report the place along the way, and the next attack starts from there: when the player
    /// could have run there, as for a move and the place of a skill.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillSyncPositionRequest request)
    {
        if (IsFighting(context.Player))
        {
            TakePlace(context.Player, request.Position, request.Yaw, SkillOpcode.SkillSyncPositionReq);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// A skill that jumps the player somewhere. As the original server does, the place is taken wherever the player jumps
    /// within a move's reach (no skill of the tables has a distance of its own that the server could check), and everybody
    /// in the maze is told where the player is (06/46). A place that is out of reach is not taken: the player alone is told
    /// the place the server keeps, and its client goes back to it.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillWarpPositionRequest request)
    {
        if (!IsFighting(context.Player))
        {
            return ValueTask.CompletedTask;
        }

        var taken = TakePlace(context.Player, request.Position, null, SkillOpcode.SkillWarpPositionReq);

        var place = ecs.Get<WorldComponent>(context.Player);
        var warp = new SkillWarpPositionBroadcastResponse
        {
            Actor = ecs.Get<ActorComponent>(context.Player),
            PosInfo = new SkillPositionValue
            {
                Position = place.Position,
                Angle = place.Rotation,
                MotionClass = NoMotion
            }
        };

        if (taken)
        {
            ecs.Get<LuaMaze>(context.Player).Creatures.Broadcast(warp);
        }
        else
        {
            ecs.Get<ServerSessionComponent>(context.Player).Send(warp);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// A place a skill reports for the player, taken when it is a place and the player could have run there since the last
    /// one. A refused place counts against the error budget once a second, as a refused move does, and the player stays.
    /// </summary>
    /// <returns>Whether the place was taken.</returns>
    private bool TakePlace(Entity player, Vector3 position, float? yaw, SkillOpcode request)
    {
        var world = ecs.Get<WorldComponent>(player);
        var rotation = yaw is { } turned && Place.IsValid(world.Position, turned) ? turned : world.Rotation;

        if (!Place.IsValid(position))
        {
            return false;
        }

        if (pace.Check(ecs, player, world.Position, position, out var charge) != MoveVerdict.Taken)
        {
            if (charge)
            {
                errors.Charge(player, MessageOpcode.From(GroupOpcode.Skill, request), HandlerErrorReason.Violation);
            }

            return false;
        }

        ecs.Set(player, world with { Position = position, Rotation = rotation });

        return true;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillPassiveRequest request)
    {
        ecs.Get<ServerSessionComponent>(context.Player).Send(new SkillPassiveResponse());

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// A passive move (evade, dash) ends where the client says, when the player could have got there.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillPassiveEndRequest request)
    {
        var place = request.Skill.Position;

        TakePlace(context.Player, place.Position, place.Angle, SkillOpcode.PassiveSkillEndReq);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillProjectileRequest request)
    {
        if (Place.IsValid(request.Position, request.Yaw) &&
            Place.IsValid(request.Right) &&
            Place.IsValid(request.Direction) &&
            ecs.Has<LuaMaze>(context.Player))
        {
            ecs.Get<LuaMaze>(context.Player).FireProjectile(context.Player, request);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// The player holds the key of the skill or lets it go. The others see it hold.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillKeypressRequest request)
    {
        if (!IsFighting(context.Player))
        {
            return ValueTask.CompletedTask;
        }

        if (Place.IsValid(Vector3.Zero, request.Rotation))
        {
            ecs.Set(context.Player, ecs.Get<WorldComponent>(context.Player) with { Rotation = request.Rotation });
        }

        ecs.Get<LuaMaze>(context.Player).PressKey(context.Player, request.IsKeyPressed);
        relay.KeyPressed(context.Player, request.IsKeyPressed);

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// The player picks another target for its skills. The others see whom it aims at.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillTargetChangeRequest request)
    {
        if (IsFighting(context.Player))
        {
            relay.TargetChanged(context.Player, request.Actor);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// The player asks for a sub animation of its skill. The others play it too.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillSubInputRequest request)
    {
        if (IsFighting(context.Player))
        {
            relay.SubInput(context.Player, request.SubAnimIndex);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// The player starts charging a skill, or ends it after holding the key for a time. The others see both.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillChargingRequest request)
    {
        if (IsFighting(context.Player))
        {
            relay.Charging(context.Player, request.Released, request.PressTime);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// The monsters a skill of the player aims at before it is played. The others see them marked.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillPreTargetListRequest request)
    {
        if (IsFighting(context.Player))
        {
            relay.PreTargets(context.Player, request.ActorList);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// The player is in a maze and on its feet: the skill input of a player that is down or that has not loaded is nobody's
    /// business.
    /// </summary>
    private bool IsFighting(Entity player)
    {
        return ecs.Has<LuaMaze>(player) && ecs.Get<LuaMaze>(player).Creatures.Vitals.IsAlive(player);
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillChainRequest request)
    {
        var chain = request.Projectile;

        if (Place.IsValid(chain.Position) && Place.IsValid(chain.Direction) && ecs.Has<LuaMaze>(context.Player))
        {
            ecs.Get<LuaMaze>(context.Player).FireChain(context.Player, chain);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// The player moves a trap with the mouse. The trap follows; the others see where it hits.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillTrapPosUpdateRequest request)
    {
        if (Place.IsValid(request.Position) && IsFighting(context.Player))
        {
            ecs.Get<LuaMaze>(context.Player).MoveTrap(context.Player, request.Position);
            relay.TrapMoved(context.Player, request.Skill, request.TriggerIndex, request.Position);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// A portal that leads to another place on this map. It shuts behind the player, and what lies beyond is alive: the
    /// portal woke it when it opened, or the arrival wakes it.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, WorldWarpRequest request)
    {
        if (!ecs.Has<LuaMaze>(context.Player))
        {
            return ValueTask.CompletedTask;
        }

        // Everyone in the maze goes through a portal together: it opens at once when all stand at it, and the others are waited
        // for otherwise (04/0F counts the seconds, 04/0E takes everybody through).
        var maze = ecs.Get<LuaMaze>(context.Player);

        // A portal is stepped into: one named from afar is a claim the client cannot make, as for the other boxes.
        if (maze.Batch.EventBox.Portals.Find(request.Portal) is { } portal && !IsAtBox(portal, ecs.Get<WorldComponent>(context.Player).Position))
        {
            LogFarFromBox(nameof(WorldWarpRequest), portal.Id);

            errors.Charge(context.Player, MessageOpcode.From(GroupOpcode.World, WorldOpcode.WarpReq), HandlerErrorReason.Violation);
            SendError(context.Player, WorldOpcode.WarpReq, WarpFailed);

            return ValueTask.CompletedTask;
        }

        if (!maze.EnterPortal(context.Player, request.Location, request.Portal, request.Jump))
        {
            SendError(context.Player, WorldOpcode.WarpReq, WarpFailed);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// The player stepped out of a portal they stood at: the others do not wait for them any more.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, WorldWarpOutRequest request)
    {
        if (ecs.Has<LuaMaze>(context.Player))
        {
            ecs.Get<LuaMaze>(context.Player).LeavePortal(context.Player);
        }

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

        if (!StandsAt(context.Player, box, MazeOpcode.eSUB_CMD_CHECK_EVENT_SPAWN_BOX_REQ, nameof(MazeCheckEventSpawnBoxRequest)))
        {
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

        // The box counts where the person stands, not where the client says it stepped.
        if (!StandsAt(context.Player, box, MazeOpcode.eSUB_CMD_MAZE_QUEST_MOVE_CHECK_REQ, nameof(MazeQuestMoveCheckRequest)))
        {
            return ValueTask.CompletedTask;
        }

        if (maze.Progress.TryUseBox(box.Id))
        {
            questManager.TryApplyConditionProgress(context.Player, box.EpisodeCondition, 1);

            // A condition that counts for the group counts for everyone in the maze, and the box is told to all of them.
            if (questManager.CountsForGroup(box.EpisodeCondition))
            {
                foreach (var other in maze.Members.Where(e => e != context.Player && ecs.IsAlive(e)))
                {
                    questManager.TryApplyConditionProgress(other, box.EpisodeCondition, 1);
                }
            }

            maze.Creatures.Broadcast(new MazeQuestMoveCheckResponse { Box = box.Id });
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// How far outside a box a person may stand and still be in it, for the moves the server has not heard yet.
    /// </summary>
    private const float BoxSlack = 1000f;

    /// <summary>
    /// Whether the person stands at the box the client names. A box named from afar is a claim the client cannot make: it
    /// does nothing and counts against the error budget of the session, as a move that is too fast does.
    /// </summary>
    private bool StandsAt(Entity player, BasicEntity box, MazeOpcode command, string request)
    {
        if (IsAtBox(box, ecs.Get<WorldComponent>(player).Position))
        {
            return true;
        }

        LogFarFromBox(request, box.Id);

        errors.Charge(player, MessageOpcode.From(GroupOpcode.Maze, command), HandlerErrorReason.Violation);

        return false;
    }

    /// <summary>
    /// Whether a place is in a box of the map, on the ground: within the half of its diagonal from its middle, with some slack.
    /// </summary>
    private static bool IsAtBox(BasicEntity box, Vector3 position)
    {
        var a = new Vector2(box.PosTopLeft.X, box.PosTopLeft.Y);
        var b = new Vector2(box.PosBottomRight.X, box.PosBottomRight.Y);
        var middle = (a + b) / 2;

        return Vector2.Distance(middle, new Vector2(position.X, position.Y)) <= Vector2.Distance(a, b) / 2 + BoxSlack;
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

        if (maze.Batch.EventBox.InterActions.Find(request.Box) is { } place &&
            !StandsAt(context.Player, place, MazeOpcode.eSUB_CMD_MAZE_INTERACTION_CLICK_REQ, nameof(MazeInteractionClickRequest)))
        {
            SendError(context.Player, MazeOpcode.eSUB_CMD_MAZE_INTERACTION_CLICK_REQ, InteractionFailed);
            return;
        }

        var run = new MazeInteractionRunResponse
        {
            Result = 0,
            Interaction = maze.Progress.UseInteraction(interaction)
        };

        // An object that is private to whoever uses it changes for that player alone; any other changes for everybody in the maze.
        if (maze.Progress.IsPrivate(request.Box))
        {
            ecs.Get<ServerSessionComponent>(context.Player).Send(run);
        }

        else
        {
            maze.Creatures.Broadcast(run);
        }

        await maze.CollectSoulObjectAsync(context.Player, request.Box);

        // A mode of the maze may be waiting for this object.
        maze.ModeInteracted(context.Player, request.Box);

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
        pace.PlaceTold(ecs, player);

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
        ecs.Get<ServerSessionComponent>(player).Send(new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.World, command), code));
    }

    private void SendError(Entity player, MazeOpcode command, ushort code)
    {
        ecs.Get<ServerSessionComponent>(player).Send(new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.Maze, command), code));
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
    /// The motion of using an object is shown to the others. It is not shown yet, and the client asks to use the object
    /// itself when the motion is over.
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
    /// The call of a party into a maze is made in a district (<c>PartyMazeEntry</c> of the district server); in a maze
    /// nobody is asked.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, MazeEnterPartyAnswerRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PersonUpdateCutsceneRequest request)
    {
        if (ecs.Has<LuaMaze>(context.Player))
        {
            await ecs.Get<LuaMaze>(context.Player).UpdateCutsceneAsync(context.Player, request.Play, request.Name);
        }
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, MazeExitRequest request)
    {
        // The way out of the last tutorial maze ends the prologue.
        await prologue.LeaveAsync(context.Player);

        if (!ecs.IsAlive(context.Player))
        {
            return;
        }

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

        // The district loads the person from the database: what the maze has must be there first.
        await persons.FlushAsync(context.Player);

        await districtReserves
            .InsertAsync(cache, WorldManager.ReservationLifetime);

        session.Send(new WorldEnterResponse { Map = enter });

        // The player is on the way out: the maze goes on for the rest of the party, and the last one out ends it.
        await instances.LeaveAsync(context.Player);
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

        if (!StandsAt(context.Player, boxes, MazeOpcode.eSUB_CMD_EVENT_SPAWN_BOX_REQ, nameof(MazeEventSpawnBoxRequest)))
        {
            return ValueTask.CompletedTask;
        }

        // The box wakes its monsters once: a request that is repeated would make them again and again.
        if (!maze.Progress.TryUseBox(boxes.Id))
        {
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

        if (!StandsAt(context.Player, function, MazeOpcode.eSUB_CMD_MAZE_LUA_FUNCTION_REQ, nameof(MazeLuaFunctionRequest)))
        {
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

    [LoggerMessage(Level = LogLevel.Debug, Message = "Skill {Skill} refused with {Code}")]
    private partial void LogSkillRefused(int skill, ushort code);

    // What a client sends is no reason to fill the log at a higher level than Debug.
    [LoggerMessage(Level = LogLevel.Debug, Message = "{Request} from a player who is not in a maze, ignored")]
    private partial void LogNotInMaze(string request);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{Request} names box {Box}, which the maze does not have, refused")]
    private partial void LogUnknownBox(string request, int box);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{Request} names box {Box}, far from where the player stands, ignored")]
    private partial void LogFarFromBox(string request, int box);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The maze {Maze} leads to location {Location}, but no district of it can take a player")]
    private partial void LogNoDistrictToExit(int maze, int location);
}
