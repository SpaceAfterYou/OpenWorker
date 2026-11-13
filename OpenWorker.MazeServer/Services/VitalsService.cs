using Arch.Core;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Gameplay.Buffs;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Maze;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Requests;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Lua;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// A player who has fallen in a maze asks to get up. The maze decides whether the player may by the way it asks
/// (<see cref="MazeRevival"/>), and answers a refusal with the code of the reference.
/// </summary>
public sealed class VitalsService(World world, MazeRevival revival) : IHotspotHandler<PersonReviveReqRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, PersonReviveReqRequest request)
    {
        if (!world.Has<LuaMaze>(context.Player))
        {
            return ValueTask.CompletedTask;
        }

        // The player is the one of the connection, whoever the request names.
        var maze = world.Get<LuaMaze>(context.Player);
        var fallen = !maze.Creatures.Vitals.IsAlive(context.Player);

        var revivable = BuffBook.Of(world, context.Player) is { } book && book.Buffs.Any(e => e.Definition.Effect == BuffEffect.Revive);

        if (revival.Check(context.Player, maze.Maze, fallen, request.Type, revivable) is { } code)
        {
            world.Get<ServerSessionComponent>(context.Player)
                .Send(new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.Character, CharacterOpcode.ReviveRes), code));

            return ValueTask.CompletedTask;
        }

        // Getting up by a buff uses the buff up; the one who gave it is told as the reviver.
        var owner = request.Type == ReviveType.Buff ? maze.Buffs.TakeRevive(context.Player) ?? 0 : 0;

        maze.Creatures.Revive(context.Player, request.Type, owner);

        return ValueTask.CompletedTask;
    }
}
