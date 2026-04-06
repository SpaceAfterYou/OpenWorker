using Arch.Core;

namespace OpenWorker.Commands;

public sealed partial class CommandManager
{
    public async ValueTask<bool> TryExecute(Entity player, string message)
    {
        var tokens = message.Split(' ');

        var trigger = tokens.FirstOrDefault();
        if (trigger is null)
        {
            return false;
        }

        if (!Commands.TryGetValue(trigger, out var command))
        {
            // TODO: Command not found message
            return false;
        }
        
        return await command.TryExecute(player, tokens[1..]).ConfigureAwait(false);
    }
}