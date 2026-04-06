using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Persons.Responses;

namespace OpenWorker.Commands.Persons;

[CommandTrigger("exp")]
[CommandDescription("Add player experience")]
internal sealed partial class PersonExpCommand : ICommand
{
    [ExternalDependency]
    private World World { get; }
    
    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!int.TryParse(tokens.ElementAtOrDefault(0), out var value))
        {
            // TODO: Failed parser message
            return ValueTask.FromResult(false);
        }

        _ = int.TryParse(tokens.ElementAtOrDefault(1), out var additional);
        _ = int.TryParse(tokens.ElementAtOrDefault(2), out var bonus);
        
        PrivateExecute(player, value, additional, bonus);
        
        return ValueTask.FromResult(true);
    }
    
    private void PrivateExecute(Entity player, int value, int additional, int bonus)
    {
        var session = World.Get<ServerSessionComponent>(player);
        
        session.Send(new PersonExpResponse { Value = value, Additional = additional, Bonus = bonus });
    }
}