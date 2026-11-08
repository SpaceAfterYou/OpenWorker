using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Modules.Quests;

namespace OpenWorker.Commands.QuestCommands;

[CommandTrigger("quest", "sector")]
[CommandDescription("Count a cleared sector for the quests: the maze and the sector")]
internal sealed partial class QuestSectorCommand : ICommand
{
    [ExternalDependency]
    private QuestManager Manager { get; }

    async ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!int.TryParse(tokens.ElementAtOrDefault(0), out var maze) ||
            !int.TryParse(tokens.ElementAtOrDefault(1), out var sector))
        {
            return false;
        }

        await Manager.OnSectorClearedAsync(player, maze, sector, CancellationToken.None);

        return true;
    }
}
