using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Modules.DailyMissions;

namespace OpenWorker.Commands.DailyMissionCommands;

[CommandTrigger("dailymission", "clear")]
[CommandDescription("Count a cleared maze for the daily missions: the maze, the rank and the seconds")]
internal sealed partial class DailyMissionClearCommand : ICommand
{
    [ExternalDependency]
    private DailyMissionManager Manager { get; }

    async ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!int.TryParse(tokens.ElementAtOrDefault(0), out var maze))
        {
            return false;
        }

        _ = int.TryParse(tokens.ElementAtOrDefault(1), out var rank);
        _ = int.TryParse(tokens.ElementAtOrDefault(2), out var seconds);

        await Manager.OnMazeClearedAsync(player, new DailyMissionMazeClear(maze, rank, seconds), false, CancellationToken.None);

        return true;
    }
}
