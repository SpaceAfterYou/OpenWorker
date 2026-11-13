using System.Globalization;
using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Lua;

namespace OpenWorker.Commands.MazeEventCommands;

/// <summary>
/// Gives the player a monster of a row to control, as a skill does: <c>/monster-control 1030101</c>; <c>/monster-control 0</c>
/// lets it go. A defensive weapon is refused with a line that says why.
/// </summary>
[CommandTrigger("monster", "control")]
[CommandDescription("Control a monster of a row, or let it go with 0")]
internal sealed partial class MonsterControlCommand : ICommand
{
    [ExternalDependency]
    private World World { get; }

    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!World.Has<LuaMaze>(player) ||
            !int.TryParse(tokens.ElementAtOrDefault(0), NumberStyles.Integer, CultureInfo.InvariantCulture, out var monster) || monster < 0)
        {
            return ValueTask.FromResult(false);
        }

        var maze = World.Get<LuaMaze>(player);

        if (maze.IsDefensiveWeapon(monster))
        {
            WeaponRefusal.Tell(World, player, monster);

            return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(maze.ControlMonster(player, monster));
    }
}
