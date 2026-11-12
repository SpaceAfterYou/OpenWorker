using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Skill.Requests;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Lua;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// A player in a maze calls a card from the quick slots of its akashic records (06/20). A refusal is the error answer of
/// 06/21 with the original's code; a call that is taken is answered with 06/21 and shown to the others with 06/22, and a card
/// that brings a creature in waits for the akashic skill of the player (<see cref="AkashicCalls"/>). A fallen player is not
/// answered, as the original does not answer one.
/// </summary>
public sealed partial class AkashicCallService(World world, AkashicCalls calls, SimulationClock clock, ILogger<AkashicCallService> logger)
    : IHotspotHandler<SkillAkashicRecordRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillAkashicRecordRequest request)
    {
        var player = context.Player;

        if (!world.Has<LuaMaze>(player))
        {
            return ValueTask.CompletedTask;
        }

        var maze = world.Get<LuaMaze>(player);

        if (!maze.Creatures.Vitals.IsAlive(player))
        {
            return ValueTask.CompletedTask;
        }

        var session = world.Get<ServerSessionComponent>(player);
        var place = world.Get<WorldComponent>(player);

        // The card turns the way the player faces when it calls it; a yaw that is not a number is the server's own.
        var position = request.SkillPosition;
        var yaw = float.IsFinite(position.Angle) ? position.Angle : place.Rotation;

        var code = calls.Call(player, request.Slot, maze.Maze, yaw, clock.Time, out var card);

        if (code != AkashicRefusal.None)
        {
            LogRefused(logger, request.Slot, code);

            session.Send(new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.Skill, SkillOpcode.AkashicRecordRes), code));

            return ValueTask.CompletedTask;
        }

        // The answer and the others carry the place the player called the card from, as the server keeps it.
        var value = default(SkillAkashicRecordValue) with
        {
            Akashic = (uint)card.Id,
            Player = world.Get<ActorComponent>(player),
            SkillPosition = position with { Position = place.Position, Angle = yaw }
        };

        session.Send(new SkillAkashicRecordResponse { Unknown = 0, Card = value });
        maze.Creatures.BroadcastToOthers(player, new SkillAkashicRecordBroadcastResponse { Card = value });

        return ValueTask.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "A call of the akashic card of quick slot {Slot} is refused with {Code}")]
    private static partial void LogRefused(ILogger logger, byte slot, ushort code);
}
