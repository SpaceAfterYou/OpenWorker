using System.Globalization;
using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Lua;

namespace OpenWorker.Commands.MazeEventCommands;

/// <summary>
/// Gives the player a monster of a row to control, as a skill does: <c>/monster-control 1030101</c>; <c>/monster-control 0</c>
/// lets it go.
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

        return ValueTask.FromResult(World.Get<LuaMaze>(player).ControlMonster(player, monster));
    }
}
