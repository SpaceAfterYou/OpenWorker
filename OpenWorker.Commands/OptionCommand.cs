using Arch.Core;
using Arch.Core.Extensions;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Login.Responses;

namespace OpenWorker.Commands;

[CommandTrigger("option")]
[CommandDescription("Set player option")]
internal sealed partial class OptionCommand : ICommand
{
    [ExternalDependency]
    private World World { get; }
    
    [ExternalDependency]
    private ServerContent Contents { get; }
    
    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!int.TryParse(tokens.ElementAtOrDefault(0), out var index) || index < 0 || index >= Contents.Count)
        {
            // TODO: Failed parser message
            return ValueTask.FromResult(false);
        }
        
        if (!bool.TryParse(tokens.ElementAtOrDefault(1), out var flag))
        {
            // TODO: Failed parser message
            return ValueTask.FromResult(false);
        }
        
        PrivateExecute(player, index, flag);
        
        return ValueTask.FromResult(true);
    }
    
    private void PrivateExecute(Entity player, int index, bool flag)
    {
        var session = World.Get<ServerSessionComponent>(player);
        
        Contents[index] = flag;
        
        var personOptions = World.Has<PersonOptionComponent>(player)
            ? World.Get<PersonOptionComponent>(player).Collection
            : [];

        var response = new LoginOptionLoadResponse { PersonOptions = personOptions, Contents = Contents };
        session.Send(response);
    }
}