using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Modules.Quests;

namespace OpenWorker.Commands.QuestCommands;

[CommandTrigger("quest", "accept")]
[CommandDescription("Take an episode as the client asks to: the episode")]
internal sealed partial class QuestAcceptCommand : ICommand
{
    [ExternalDependency]
    private QuestManager Manager { get; }

    async ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!int.TryParse(tokens.ElementAtOrDefault(0), out var episode))
        {
            return false;
        }

        return await Manager.AcceptAsync(player, episode, CancellationToken.None);
    }
}
