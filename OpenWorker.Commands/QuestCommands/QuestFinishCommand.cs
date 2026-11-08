using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Modules.Quests;

namespace OpenWorker.Commands.QuestCommands;

[CommandTrigger("quest", "finish")]
[CommandDescription("Finish an episode as a script does, whether or not its conditions are done: the episode")]
internal sealed partial class QuestFinishCommand : ICommand
{
    [ExternalDependency]
    private QuestManager Manager { get; }

    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!int.TryParse(tokens.ElementAtOrDefault(0), out var episode))
        {
            return ValueTask.FromResult(false);
        }

        Manager.CompleteEpisode(player, episode);

        return ValueTask.FromResult(true);
    }
}
