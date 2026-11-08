using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Modules.Quests;

namespace OpenWorker.Commands.QuestCommands;

[CommandTrigger("quest", "force")]
[CommandDescription("Take an episode as a script does, without the checks of the episodes before it: the episode")]
internal sealed partial class QuestForceCommand : ICommand
{
    [ExternalDependency]
    private QuestManager Manager { get; }

    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!int.TryParse(tokens.ElementAtOrDefault(0), out var episode))
        {
            return ValueTask.FromResult(false);
        }

        Manager.AcceptEpisode(player, episode);

        return ValueTask.FromResult(true);
    }
}
