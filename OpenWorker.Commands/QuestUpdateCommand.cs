using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Quests.Responses;
using OpenWorker.Hotspot.Modules.Quests.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Commands;

[CommandTrigger("quest", "update")]
[CommandDescription("Update player quest progress")]
internal sealed partial class QuestUpdateCommand : ICommand
{
    [ExternalDependency]
    private World World { get; }
    
    [ExternalDependency]
    private ReadOnlyCollection<QuestConditionRow> ConditionCollection { get; }
    
    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!int.TryParse(tokens.ElementAtOrDefault(0), out var condition))
        {
            // TODO: Failed parser message
            return ValueTask.FromResult(false);
        }

        if (!byte.TryParse(tokens.ElementAtOrDefault(1), out var step))
        {
            // TODO: Failed parser message
            return ValueTask.FromResult(false);
        }

        if (ConditionCollection.All(e => e.Id != condition))
        {
            // TODO: Not found message
            return ValueTask.FromResult(false);
        }

        if (step > ConditionCollection.First(e => e.Id == condition).Field21)
        {
            // TODO: Invalid step message
            return ValueTask.FromResult(false);
        }
        
        PrivateExecute(player, condition, step);
        
        return ValueTask.FromResult(true);
    }
    
    private void PrivateExecute(Entity player, int condition, byte step)
    {
        var session = World.Get<ServerSessionComponent>(player);

        session.Send(new QuestUpdateResponse
        {
            ConditionList =
            [
                new QuestCondition
                {
                    Condition = condition,
                    Step = step
                }
            ]
        });
    }
}