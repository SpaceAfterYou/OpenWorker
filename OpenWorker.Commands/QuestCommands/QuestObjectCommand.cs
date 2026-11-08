using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Modules.Quests;

namespace OpenWorker.Commands.QuestCommands;

[CommandTrigger("quest", "object")]
[CommandDescription("Count a used object of a maze for the quests: the box")]
internal sealed partial class QuestObjectCommand : ICommand
{
    [ExternalDependency]
    private QuestManager Manager { get; }

    async ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!int.TryParse(tokens.ElementAtOrDefault(0), out var box))
        {
            return false;
        }

        await Manager.OnObjectUsedAsync(player, box, CancellationToken.None);

        return true;
    }
}
