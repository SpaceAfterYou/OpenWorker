using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Domain.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Persons.Responses;

namespace OpenWorker.Commands.Persons;

[CommandTrigger("die")]
[CommandDescription("Kill player")]
internal sealed partial class PersonDieCommand : ICommand
{
    [ExternalDependency]
    private World World { get; }
    
    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        _ = int.TryParse(tokens.ElementAtOrDefault(0), out var pvpKillCount);
        
        PrivateExecute(player, pvpKillCount);
        
        return ValueTask.FromResult(true);
    }
    
    private void PrivateExecute(Entity player, int pvpKillCount)
    {
        var session = World.Get<ServerSessionComponent>(player);
        var actor = World.Get<ActorComponent>(player);

        session.Send(new PersonDieResponse { Victim = actor, Attacker = actor, PvpKillCount = pvpKillCount });
    }
}