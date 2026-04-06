using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Domain.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.SoulMetry.Responses;
using OpenWorker.Hotspot.Modules.SoulMetry.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Commands;

[CommandTrigger("soulmetry", "update")]
[CommandDescription("Update player soul metry progress")]
internal sealed partial class SoulMetryUpdateCommand : ICommand
{
    [ExternalDependency]
    private World World { get; }
    
    [ExternalDependency]
    private ReadOnlyCollection<SoulMetryRow> SoulMetryCollection { get; }
    
    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!int.TryParse(tokens.ElementAtOrDefault(0), out var id))
        {
            // TODO: Failed parser message
            return ValueTask.FromResult(false);
        }

        if (SoulMetryCollection.All(e => e.Id != id))
        {
            // TODO: Not found message
            return ValueTask.FromResult(false);
        }

        if (!short.TryParse(tokens.ElementAtOrDefault(1), out var index))
        {
            // TODO: Failed parser message
            return ValueTask.FromResult(false);
        }

        if (!bool.TryParse(tokens.ElementAtOrDefault(2), out var isCompleted))
        {
            // TODO: Failed parser message
            return ValueTask.FromResult(false);
        }
        
        PrivateExecute(player, id, index, isCompleted);
        
        return ValueTask.FromResult(true);
    }
    
    private void PrivateExecute(Entity player, int id, short index, bool isCompleted)
    {
        var session = World.Get<ServerSessionComponent>(player);
        var value = SoulMetry.ChangeEpisode(0, index, isCompleted);
        
        session.Send(new SoulMetryUpdateResponse { Data = new SoulMetryValue(id, value) });
    }
}