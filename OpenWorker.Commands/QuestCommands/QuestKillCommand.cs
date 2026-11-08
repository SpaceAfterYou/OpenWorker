using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Modules.Quests;

namespace OpenWorker.Commands.QuestCommands;

[CommandTrigger("quest", "kill")]
[CommandDescription("Count a killed monster for the quests: the monster")]
internal sealed partial class QuestKillCommand : ICommand
{
    [ExternalDependency]
    private QuestManager Manager { get; }

    async ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!int.TryParse(tokens.ElementAtOrDefault(0), out var monster))
        {
            return false;
        }

        await Manager.OnMonsterKilledAsync(player, monster, CancellationToken.None);

        return true;
    }
}
