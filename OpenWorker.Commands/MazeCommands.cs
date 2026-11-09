using System.Globalization;
using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Modules.Drops;
using OpenWorker.Gameplay.Modules.Maze;
using OpenWorker.Gameplay.Modules.Maze.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Progress;

namespace OpenWorker.Commands;

/// <summary>
/// Records clears of a maze and of every maze before it for the person, as if it had cleared them, so that the maze may be
/// entered: <c>/maze-clear 21311 [rank]</c>. A later episode asks for the one before it to be cleared.
/// </summary>
[CommandTrigger("maze-clear")]
[CommandDescription("Record a clear of a maze")]
internal sealed partial class MazeClearCommand : ICommand
{
    [ExternalDependency]
    private SharePointManager Progress { get; }

    [ExternalDependency]
    private MazeScoring Scoring { get; }

    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!int.TryParse(tokens.ElementAtOrDefault(0), NumberStyles.Integer, CultureInfo.InvariantCulture, out var maze) || maze <= 0)
        {
            return ValueTask.FromResult(false);
        }

        var rank = byte.TryParse(tokens.ElementAtOrDefault(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var given) ? Math.Min(given, (byte)5) : (byte)0;

        // The maze and the chain of the mazes each asks for, as far as the server's rows of the mazes name them.
        var cleared = new HashSet<int>();

        for (var current = maze; current > 0 && cleared.Add(current);)
        {
            Progress.RecordClear(player, current, rank, 0);

            current = Scoring.TryGetRule(current, out var rule) && rule.RequiredMaze <= int.MaxValue ? (int)rule.RequiredMaze : 0;
        }

        return ValueTask.FromResult(Progress.HasCleared(player, maze));
    }
}

/// <summary>
/// Sets the fatigue of the person: <c>/fatigue 200 [saved]</c>.
/// </summary>
[CommandTrigger("fatigue")]
[CommandDescription("Set the fatigue of the person")]
internal sealed partial class FatigueCommand : ICommand
{
    [ExternalDependency]
    private World World { get; }

    [ExternalDependency]
    private FatigueService Fatigue { get; }

    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!short.TryParse(tokens.ElementAtOrDefault(0), NumberStyles.Integer, CultureInfo.InvariantCulture, out var common) ||
            common is < 0 or > FatigueService.MaxCommon || !Fatigue.Refresh(player, out var fatigue))
        {
            return ValueTask.FromResult(false);
        }

        var bonus = short.TryParse(tokens.ElementAtOrDefault(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var saved)
            ? Math.Clamp(saved, (short)0, FatigueService.MaxBonus)
            : fatigue.Bonus;

        World.Set(player, fatigue with { Common = common, Bonus = bonus });
        World.MarkDirty(player, PersonSections.Currencies);
        Fatigue.Send(player);

        return ValueTask.FromResult(true);
    }
}
