using Arch.Core;
using OpenWorker.Hotspot.Commands;

namespace OpenWorker.Commands;

public sealed class StuffCommands(IReadOnlyDictionary<string, AStuffCommand> commands)
{
    public async ValueTask<bool> TryExecute(Entity entity, string message)
    {
        var tokens = message.Split(' ');

        var trigger = tokens.FirstOrDefault();
        if (trigger is null)
        {
            return false;
        }

        return commands.TryGetValue(trigger, out var command) && await command
            .TryExecute(entity, tokens[1..])
            .ConfigureAwait(false);
    }
}