using System.Globalization;
using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Lua;

namespace OpenWorker.Commands.MazeEventCommands;

/// <summary>
/// Calls monsters of a row of the monster table in where the player stands: <c>/monster 30103001 [count]</c>. Up to 50 at once.
/// A defensive weapon is refused with a line that says why.
/// </summary>
[CommandTrigger("monster")]
[CommandDescription("Call monsters of a row in where you stand")]
internal sealed partial class MonsterCommand : ICommand
{
    private const int MaxCount = 50;

    [ExternalDependency]
    private World World { get; }

    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!World.Has<LuaMaze>(player) ||
            !int.TryParse(tokens.ElementAtOrDefault(0), NumberStyles.Integer, CultureInfo.InvariantCulture, out var monster) || monster <= 0)
        {
            return ValueTask.FromResult(false);
        }

        var count = 1;

        if (tokens.Length > 1 &&
            (!int.TryParse(tokens[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out count) || count is < 1 or > MaxCount))
        {
            return ValueTask.FromResult(false);
        }

        var maze = World.Get<LuaMaze>(player);

        if (maze.IsDefensiveWeapon(monster))
        {
            WeaponRefusal.Tell(World, player, monster);

            return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(maze.CallMonsters(player, monster, count));
    }
}
