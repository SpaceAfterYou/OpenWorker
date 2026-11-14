using Arch.Core;
using System.Numerics;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Types;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Modules.Npcs.Components;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Movement.Requests;
using OpenWorker.Hotspot.Modules.Movement.Responses;
using OpenWorker.Hotspot.Modules.Persons.Requests;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Wire;
using Microsoft.Extensions.Logging;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler;
using OpenWorker.Hotspot.Modules.World.Enums;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Hotspot.Messages.Response.Person;

namespace OpenWorker.Channel;

/// <summary>
/// Keeps the place of a person and shows its movement to the others on the map. The sender gets its
/// own movement too: the client skips what it has already done and starts looped motions only from here.<br/>
/// A move that takes the player too far in one message (<see cref="MovementPace"/>) is refused: the place stays, nobody
/// else hears of it, and the player is warped back to the place the server keeps. The first refusal in a second counts
/// against the error budget of the session. A move that names another map than the one the player is in is from the map
/// it left, and is dropped as the original server drops it; so is one from the place the client left before it took the
/// place it was told.<br/>
/// A player on a ride of an NPC of transports (<see cref="TransportRideComponent"/>) is carried by the ride: what its client
/// reports meanwhile is not a move.
/// </summary>
public abstract partial class MovementServiceBase(World world, MovementPace pace, HandlerExecutor errors, ILogger logger) :
    IHotspotHandler<MovementMoveRequest>,
    IHotspotHandler<MovementStopRequest>,
    IHotspotHandler<MovementJumpRequest>,
    IHotspotHandler<MovementBattleRequest>,
    IHotspotHandler<MovementMotionRequest>,
    IHotspotHandler<MovementIgnoreMotionDeltaRequest>,
    IHotspotHandler<MovementTransportOffRequest>,
    IHotspotHandler<MovementRotationRequest>,
    IHotspotHandler<MovementPositionRequest>,
    IHotspotHandler<MovementLoopMotionStartRequest>,
    IHotspotHandler<MovementLoopMotionEndRequest>,
    IHotspotHandler<MovementAttacedEndRequest>,
    IHotspotHandler<MovementGroundStatusRequest>,
    IHotspotHandler<MovementJumpQuickDownRequest>,
    IHotspotHandler<PersonChangeMotionReqRequest>
{
    /// <summary>
    /// Sends to everybody who can see the player, including the player.
    /// </summary>
    protected abstract void Broadcast<T>(Entity player, in T message) where T : struct, IResponseHotspotMessage, IWireWritable;

    protected abstract void BroadcastToOthers<T>(Entity player, in T message) where T : struct, IResponseHotspotMessage, IWireWritable;

    /// <summary>
    /// A move of the player, which the next one may replace before it goes out: by default it goes at once.
    /// </summary>
    protected virtual void Move(Entity player, in MovementMoveBtResponse message)
    {
        Broadcast(player, in message);
    }

    /// <summary>
    /// The place of the player has changed: where the others see it from may have too.
    /// </summary>
    protected virtual void OnPlaced(Entity player, Vector3 position)
    {
    }

    /// <summary>
    /// The player jumped, and the others were told.
    /// </summary>
    protected virtual void OnJumped(Entity player)
    {
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PersonChangeMotionReqRequest request)
    {
        // The motion after a blow says whether the player lies on the ground.
        var stance = new PlayerStanceComponent(request.Motion, request.SubMotion);

        if (world.Has<PlayerStanceComponent>(context.Player))
        {
            world.Set(context.Player, stance);
        }
        else
        {
            world.Add(context.Player, stance);
        }

        BroadcastToOthers(context.Player, new PersonChangeMotionBroadcastResponse
        {
            Actor = world.Get<ActorComponent>(context.Player).Identifier,
            Motion = request.Motion,
            SubMotion = request.SubMotion
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementMoveRequest request)
    {
        var position = new Vector3(request.fPosX, request.fPosY, request.fPosZ);

        if (TryUpdatePlace(context.Player, request.Opcode, request.dwActorID, position, request.fYaw, request.nMapID) &&
            float.IsFinite(request.fTargetPosX) &&
            float.IsFinite(request.fTargetPosY))
        {
            Move(context.Player, new MovementMoveBtResponse
            {
                Actor = request.dwActorID,
                Map = request.nMapID,
                Pos = position,
                Yaw = request.fYaw,
                TargetPosX = request.fTargetPosX,
                TargetPosY = request.fTargetPosY,
                RunBit = request.byRunBit,
                Pitch = request.fPitch,
                MoveSpeed = request.fMoveSpeed,
                ChangeMotion = request.byChangeMotion
            });
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementStopRequest request)
    {
        var position = new Vector3(request.fPosX, request.fPosY, request.fPosZ);

        if (TryUpdatePlace(context.Player, request.Opcode, request.dwActorID, position, request.fYaw, request.nMapID))
        {
            Broadcast(context.Player, new MovementStopBtResponse
            {
                Actor = request.dwActorID,
                MapId = request.nMapID,
                PosX = request.fPosX,
                PosY = request.fPosY,
                PosZ = request.fPosZ,
                Yaw = request.fYaw,
                Pitch = request.fPitch
            });
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementJumpRequest request)
    {
        var position = new Vector3(request.fPosX, request.fPosY, request.fPosZ);

        if (TryUpdatePlace(context.Player, request.Opcode, request.dwActorID, position, request.fYaw, request.nMapID) &&
            float.IsFinite(request.fTargetPosX) &&
            float.IsFinite(request.fTargetPosY))
        {
            Broadcast(context.Player, new MovementJumpBtResponse
            {
                Actor = request.dwActorID,
                MapId = request.nMapID,
                PosX = request.fPosX,
                PosY = request.fPosY,
                PosZ = request.fPosZ,
                Yaw = request.fYaw,
                TargetPosX = request.fTargetPosX,
                TargetPosY = request.fTargetPosY,
                JumpingMove = request.bJumpingMove,
                JumpDrop = request.bJumpDrop
            });

            OnJumped(context.Player);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementPositionRequest request)
    {
        // Only tells the server where the person is, the others do not need it.
        TryUpdatePlace(
            context.Player,
            request.Opcode,
            request.dwActorID,
            new Vector3(request.fPosX, request.fPosY, request.fPosZ),
            request.fYaw,
            request.nMapID);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementIgnoreMotionDeltaRequest request)
    {
        var position = new Vector3(request.fPosX, request.fPosY, request.fPosZ);

        if (TryUpdatePlace(context.Player, request.Opcode, request.dwActorID, position, request.fYaw))
        {
            Broadcast(context.Player, new MovementIgnoreMotionDeltaResponse
            {
                Actor = request.dwActorID,
                Pos = position,
                Yaw = request.fYaw,
                Pitch = request.fPitch
            });
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementBattleRequest request)
    {
        var position = new Vector3(request.fPosX, request.fPosY, request.fPosZ);

        if (TryUpdatePlace(context.Player, request.Opcode, request.dwActorID, position, request.fYaw))
        {
            Broadcast(context.Player, new MovementBattleBtResponse
            {
                Actor = request.dwActorID,
                Pos = position,
                Yaw = request.fYaw,
                BattlePose = request.bBattlePose,
                PlayMotion = (byte)request.bPlayMotion
            });
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementMotionRequest request)
    {
        if (IsOwnActor(context.Player, request.dwActorID))
        {
            Broadcast(context.Player, new MovementMotionBtResponse
            {
                Actor = request.dwActorID,
                MotionClass = request.nMotionClass,
                SubClass = request.nSubClass
            });
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementLoopMotionStartRequest request)
    {
        var position = new Vector3(request.fPosX, request.fPosY, request.fPosZ);

        if (TryUpdatePlace(context.Player, request.Opcode, request.dwActorID, position, request.fYaw))
        {
            Broadcast(context.Player, new MovementLoopMotionStartBtResponse
            {
                Actor = request.dwActorID,
                Pos = position,
                Yaw = request.fYaw,
                StartAnimation = request.dwStartAniID,
                LoopAnimation = request.dwLoopAniID,
                EndAnimation = request.dwEndAniID,
                IdleAnimation = request.dwIdleAniID
            });
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementLoopMotionEndRequest request)
    {
        if (IsOwnActor(context.Player, request.dwActorID))
        {
            Broadcast(context.Player, new MovementLoopMotionEndBtResponse { Actor = request.dwActorID });
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementAttacedEndRequest request)
    {
        var place = world.Has<WorldComponent>(context.Player) ? world.Get<WorldComponent>(context.Player).Rotation : 0;
        var position = new Vector3(request.fPosX, request.fPosY, request.fPosZ);

        if (TryUpdatePlace(context.Player, request.Opcode, request.dwActorID, position, place))
        {
            BroadcastToOthers(context.Player, new MovementAttacedEndBtResponse { Actor = request.dwActorID });
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// The client says whether the player stands on the ground and how high the ground is. The server keeps it as the
    /// original does (<see cref="GroundStateComponent"/>) and tells nobody.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementGroundStatusRequest request)
    {
        if (IsOwnActor(context.Player, request.dwActorID) && float.IsFinite(request.fValue))
        {
            var state = new GroundStateComponent(request.nStatus != 0, request.fValue);

            if (world.Has<GroundStateComponent>(context.Player))
            {
                world.Set(context.Player, state);
            }

            else
            {
                world.Add(context.Player, state);
            }
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementTransportOffRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementRotationRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementJumpQuickDownRequest request)
    {
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// A person moves only itself, and only after entering the game through this connection.
    /// </summary>
    private bool IsOwnActor(Entity player, ActorValue actor)
    {
        return world.Get<ClaimsComponent>(player).Account != 0 &&
               world.Get<ActorComponent>(player).Identifier == actor.Identifier;
    }

    /// <param name="map">The map the request names, for the requests that carry one.</param>
    private bool TryUpdatePlace(Entity player, MessageOpcode opcode, ActorValue actor, Vector3 position, float rotation, MapValue? map = null)
    {
        // A rider is carried by the ride, not by its client: what the client reports along the path is not a move.
        if (!IsOwnActor(player, actor) ||
            !Place.IsValid(position, rotation) ||
            world.Has<TransportRideComponent>(player))
        {
            return false;
        }

        var place = world.Get<WorldComponent>(player);

        // A move of the map the client has left: the original drops it whatever the place.
        if (map is { } named && named.Location != place.Location)
        {
            return false;
        }

        switch (pace.Check(world, player, place.Position, position, out var charge))
        {
            case MoveVerdict.Refused:
                Refuse(player, opcode, place, position, charge);

                return false;

            case MoveVerdict.Stale:
                return false;
        }

        world.Set(player, place with { Position = position, Rotation = rotation });

        OnPlaced(player, position);

        return true;
    }

    /// <summary>
    /// Warps the player back to where the server has it. The client takes a warp within the map from the server at any
    /// time; nobody else saw the place it claimed.
    /// </summary>
    private void Refuse(Entity player, MessageOpcode opcode, WorldComponent place, Vector3 claimed, bool charge)
    {
        var actor = world.Get<ActorComponent>(player);
        var distance = Vector3.Distance(place.Position, claimed);

        LogRefused(logger, actor.Identifier, distance, charge);

        world.Get<ServerSessionComponent>(player).Send(new WorldWarpResponse
        {
            Actor = actor,
            Result = WorldWrapResult.Ok,
            Position = place.Position,
            Rotation = place.Rotation
        });

        if (charge)
        {
            errors.Charge(player, opcode, HandlerErrorReason.Violation);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Refused a move of actor {Actor} over {Distance} units, too far in one message; counted against the budget: {Charged}.")]
    private static partial void LogRefused(ILogger logger, int actor, float distance, bool charged);
}
