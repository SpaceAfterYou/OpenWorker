using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Modules.Quests;

namespace OpenWorker.Commands;

[CommandTrigger("quest", "update")]
[CommandDescription("Update player quest progress")]
internal sealed partial class QuestUpdateCommand : ICommand
{
    [ExternalDependency]
    private QuestManager QuestManager { get; }

    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!int.TryParse(tokens.ElementAtOrDefault(0), out var condition))
        {
            return ValueTask.FromResult(false);
        }

        if (!byte.TryParse(tokens.ElementAtOrDefault(1), out var step))
        {
            return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(QuestManager.TryApplyConditionProgress(player, condition, step));
    }
}
