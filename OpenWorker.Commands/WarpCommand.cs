using Arch.Core;
using OpenWorker.Channel;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;

namespace OpenWorker.Commands;

[CommandTrigger("warp")]
[CommandDescription("Warp player to location")]
internal sealed partial class WarpCommand : ICommand
{
    [ExternalDependency]
    private WorldManager WorldManager { get; }
    
    async ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!short.TryParse(tokens.ElementAtOrDefault(0), out var id))
        {
            // TODO: Failed parser message
            return false;
        }

        await WorldManager
            .TryEnter(player, id)
            .ConfigureAwait(false);
        
        return true;
    }
}