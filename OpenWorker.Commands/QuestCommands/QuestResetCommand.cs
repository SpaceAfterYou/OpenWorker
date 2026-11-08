using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Modules.Quests;

namespace OpenWorker.Commands.QuestCommands;

[CommandTrigger("quest", "reset")]
[CommandDescription("Forget an episode, taken or finished: the episode, or nothing to forget all the quests")]
internal sealed partial class QuestResetCommand : ICommand
{
    [ExternalDependency]
    private QuestManager Manager { get; }

    async ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (tokens.Length == 0)
        {
            return await Manager.ResetAllAsync(player);
        }

        if (!int.TryParse(tokens[0], out var episode))
        {
            return false;
        }

        return await Manager.ResetEpisodeAsync(player, episode);
    }
}
