using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Modules.Quests;

namespace OpenWorker.Commands.QuestCommands;

[CommandTrigger("quest", "item")]
[CommandDescription("Count collected items for the quests: the item and how many, one if left out")]
internal sealed partial class QuestItemCommand : ICommand
{
    [ExternalDependency]
    private QuestManager Manager { get; }

    async ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!int.TryParse(tokens.ElementAtOrDefault(0), out var item))
        {
            return false;
        }

        _ = short.TryParse(tokens.ElementAtOrDefault(1), out var count);

        await Manager.OnItemCollectedAsync(player, item, count == 0 ? (short)1 : count, CancellationToken.None);

        return true;
    }
}
