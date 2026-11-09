using System.Globalization;
using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Domain.Components;
using OpenWorker.Lua;

namespace OpenWorker.Commands.BuffCommands;

/// <summary>
/// Gives a buff of the buff table in the maze, for sure, as a script does: <c>/buff-give [buff] [monster]</c>. Without a
/// monster (its actor number) it is for the player.
/// </summary>
[CommandTrigger("buff", "give")]
[CommandDescription("Give a buff to yourself or to a monster of the maze")]
internal sealed partial class BuffGiveCommand : ICommand
{
    [ExternalDependency]
    private World World { get; }

    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!World.Has<LuaMaze>(player) ||
            !int.TryParse(tokens.ElementAtOrDefault(0), NumberStyles.Integer, CultureInfo.InvariantCulture, out var buff))
        {
            return ValueTask.FromResult(false);
        }

        var maze = World.Get<LuaMaze>(player);
        var target = player;

        if (tokens.Length > 1)
        {
            if (!int.TryParse(tokens[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var actor) || maze.Creatures.FindMonster(actor) is not { } monster)
            {
                return ValueTask.FromResult(false);
            }

            target = monster;
        }

        return ValueTask.FromResult(maze.Buffs.Give(target, buff, (int)World.Get<ActorComponent>(player), roll: false));
    }
}
