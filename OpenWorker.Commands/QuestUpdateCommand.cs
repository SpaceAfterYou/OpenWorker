using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Hotspot.Commands.Attributes;
using OpenWorker.Hotspot.Modules.Quests.Responses;
using OpenWorker.Hotspot.Modules.Quests.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Commands;

[StuffCommand("quest", "update")]
public sealed class QuestUpdateCommand(World world, ReadOnlyCollection<QuestConditionRow> conditionCollection) : AStuffCommand(world)
{
    protected override string GetTutorialMessage() => "quest-update (condition) (step)";

    public override ValueTask<bool> TryExecute(Entity player, IReadOnlyList<string> tokens)
    {
        var session = World.Get<ServerSessionComponent>(player);

        if (int.TryParse(tokens.ElementAtOrDefault(0), out var condition) is false)
        {
            SendTutorial(player, nameof(condition));
            return ValueTask.FromResult(false);
        }

        if (byte.TryParse(tokens.ElementAtOrDefault(1), out var step) is false)
        {
            SendTutorial(player, nameof(step));
            return ValueTask.FromResult(false);
        }

        if (conditionCollection.Any(e => e.Id == condition) is false)
        {
            SendTutorial(player, nameof(condition));
            return ValueTask.FromResult(false);
        }

        if (step > conditionCollection.First(e => e.Id == condition).Field21)
        {
            SendTutorial(player, nameof(step));
            return ValueTask.FromResult(false);
        }

        session.Send(new QuestUpdateResponse([
            new QuestCondition
            {
                Condition = condition,
                Step = step
            }
        ]));

        return ValueTask.FromResult(true);
    }
}