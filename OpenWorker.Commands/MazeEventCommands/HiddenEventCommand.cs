using System.Globalization;
using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Lua;

namespace OpenWorker.Commands.MazeEventCommands;

/// <summary>
/// Starts the hidden event of a box at once, with one of the conditions of its row: <c>/hidden-event 6001 600101</c>. Refused
/// while the maze has an event already, after its clear, and for a condition the row does not name.
/// </summary>
[CommandTrigger("hidden", "event")]
[CommandDescription("Start the hidden event of a box now with a condition of its row")]
internal sealed partial class HiddenEventCommand : ICommand
{
    [ExternalDependency]
    private World World { get; }

    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (tokens.Length != 2 || !World.Has<LuaMaze>(player) ||
            !int.TryParse(tokens[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var box) ||
            !int.TryParse(tokens[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var condition))
        {
            return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(World.Get<LuaMaze>(player).StartHiddenEvent(player, box, condition));
    }
}
