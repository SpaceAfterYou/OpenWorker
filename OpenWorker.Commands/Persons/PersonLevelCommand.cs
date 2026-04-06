using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Domain.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Persons.Responses;

namespace OpenWorker.Commands.Persons;

[CommandTrigger("level")]
[CommandDescription("Set player level")]
internal sealed partial class PersonLevelCommand : ICommand
{
    [ExternalDependency]
    private World World { get; }
    
    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!byte.TryParse(tokens.ElementAtOrDefault(0), out var level))
        {
            // TODO: Failed parser message
            return ValueTask.FromResult(false);
        }
        
        PrivateExecute(player, level);
        
        return ValueTask.FromResult(true);
    }
    
    private void PrivateExecute(Entity player, byte level)
    {
        var session = World.Get<ServerSessionComponent>(player);
        var actor = World.Get<ActorComponent>(player);

        session.Send(new PersonLevelResponse { Actor = actor, Value = level });
    }
}