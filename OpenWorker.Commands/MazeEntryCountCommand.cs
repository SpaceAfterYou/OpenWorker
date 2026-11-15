using System.Globalization;
using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Modules.Maze;

namespace OpenWorker.Commands;

/// <summary>
/// Sets how often the person has entered a maze today, as <c>/maze-count 21061 10</c>, or clears every count as a new game
/// day does, as <c>/maze-count reset</c>.
/// </summary>
[CommandTrigger("maze-count")]
[CommandDescription("Set the entries of a maze today, or reset them all")]
internal sealed partial class MazeEntryCountCommand : ICommand
{
    [ExternalDependency]
    private MazeEntryLimits Limits { get; }

    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (tokens.ElementAtOrDefault(0) == "reset")
        {
            return ValueTask.FromResult(Limits.StartNewDay(player));
        }

        if (!int.TryParse(tokens.ElementAtOrDefault(0), NumberStyles.None, CultureInfo.InvariantCulture, out var maze) ||
            !int.TryParse(tokens.ElementAtOrDefault(1), NumberStyles.None, CultureInfo.InvariantCulture, out var count) ||
            count > ushort.MaxValue)
        {
            return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(Limits.SetCount(player, maze, count));
    }
}
