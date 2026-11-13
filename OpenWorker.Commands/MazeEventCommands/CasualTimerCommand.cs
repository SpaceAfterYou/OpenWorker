using System.Globalization;
using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Lua;

namespace OpenWorker.Commands.MazeEventCommands;

/// <summary>
/// Shows the casual raid countdown to the maze: <c>/casual-timer 60 [added]</c> counts 60 seconds down (with the seconds
/// added shown); <c>/casual-timer -1</c> hides it.
/// </summary>
[CommandTrigger("casual", "timer")]
[CommandDescription("Show the casual raid countdown for some seconds, or hide it with -1")]
internal sealed partial class CasualTimerCommand : ICommand
{
    private const float MaxSeconds = 86_400f;
    private const float Hidden = -1f;

    [ExternalDependency]
    private World World { get; }

    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!World.Has<LuaMaze>(player) ||
            !float.TryParse(tokens.ElementAtOrDefault(0), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) ||
            !float.IsFinite(seconds) || seconds == 0f || seconds > MaxSeconds)
        {
            return ValueTask.FromResult(false);
        }

        var maze = World.Get<LuaMaze>(player);

        if (seconds < 0f)
        {
            maze.ShowCasualRaidTimer((int)Hidden, Hidden);

            return ValueTask.FromResult(true);
        }

        var added = 0f;

        if (tokens.Length > 1 &&
            (!float.TryParse(tokens[1], NumberStyles.Float, CultureInfo.InvariantCulture, out added) || !float.IsFinite(added) || added is < 0f or > MaxSeconds))
        {
            return ValueTask.FromResult(false);
        }

        maze.ShowCasualRaidTimer(0, seconds, added);

        return ValueTask.FromResult(true);
    }
}
