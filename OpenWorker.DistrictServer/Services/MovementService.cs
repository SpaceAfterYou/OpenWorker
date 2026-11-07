using Arch.Core;
using OpenWorker.Channel;
using System.Numerics;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Movement.Requests;
using OpenWorker.Hotspot.Modules.Movement.Responses;
using OpenWorker.Domain.Types;

namespace OpenWorker.DistrictServer.Services;

/// <summary>
/// Keeps the place of a person and shows its movement to the channel. The sender gets its own
/// movement too: the client skips what it has already done and starts looped motions only from here.
/// </summary>
[HotspotHandler(HotspotHandlerType.District)]
public sealed class MovementService(World world, ServiceChannels channels) :
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
    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementMoveRequest request)
    {
        var position = new Vector3(request.fPosX, request.fPosY, request.fPosZ);

        if (TryUpdatePlace(context.Player, request.dwActorID, position, request.fYaw) &&
            float.IsFinite(request.fTargetPosX) &&
            float.IsFinite(request.fTargetPosY))
        {
            Broadcast(context.Player, new MovementMoveBtResponse
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

        if (TryUpdatePlace(context.Player, request.dwActorID, position, request.fYaw))
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

        if (TryUpdatePlace(context.Player, request.dwActorID, position, request.fYaw) &&
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
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementPositionRequest request)
    {
        // Only tells the server where the person is, the others do not need it.
        TryUpdatePlace(
            context.Player,
            request.dwActorID,
            new Vector3(request.fPosX, request.fPosY, request.fPosZ),
            request.fYaw);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementIgnoreMotionDeltaRequest request)
    {
        var position = new Vector3(request.fPosX, request.fPosY, request.fPosZ);

        if (TryUpdatePlace(context.Player, request.dwActorID, position, request.fYaw))
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

        if (TryUpdatePlace(context.Player, request.dwActorID, position, request.fYaw))
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

        if (TryUpdatePlace(context.Player, request.dwActorID, position, request.fYaw))
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

        if (TryUpdatePlace(context.Player, request.dwActorID, position, place))
        {
            channels.Get(context.Player).BroadcastToOthers(
                context.Player,
                new MovementAttacedEndBtResponse { Actor = request.dwActorID });
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementGroundStatusRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementTransportTakeRequest request)
    {
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

    private void Broadcast(Entity player, IResponseHotspotMessage message)
    {
        channels.Get(player).Broadcast(message);
    }

    /// <summary>
    /// A person moves only itself, and only after entering the game through this connection.
    /// </summary>
    private bool IsOwnActor(Entity player, ActorValue actor)
    {
        return world.Get<ClaimsComponent>(player).Account != 0 &&
               world.Get<ActorComponent>(player).Identifier == actor.Identifier;
    }

    private bool TryUpdatePlace(Entity player, ActorValue actor, Vector3 position, float rotation)
    {
        if (!IsOwnActor(player, actor) ||
            !float.IsFinite(position.X) ||
            !float.IsFinite(position.Y) ||
            !float.IsFinite(position.Z) ||
            rotation is not (>= -360f and <= 360f))
        {
            return false;
        }

        world.Set(player, world.Get<WorldComponent>(player) with { Position = position, Rotation = rotation });

        return true;
    }
}
