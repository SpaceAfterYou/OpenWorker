using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Modules.Quests;

namespace OpenWorker.Commands.QuestCommands;

[CommandTrigger("quest", "clear")]
[CommandDescription("Count a cleared maze for the quests: the maze")]
internal sealed partial class QuestClearCommand : ICommand
{
    [ExternalDependency]
    private QuestManager Manager { get; }

    async ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!int.TryParse(tokens.ElementAtOrDefault(0), out var maze))
        {
            return false;
        }

        await Manager.OnMazeClearedAsync(player, maze, CancellationToken.None);

        return true;
    }
}
