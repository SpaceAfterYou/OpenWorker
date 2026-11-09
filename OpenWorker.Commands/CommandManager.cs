using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Hotspot.Enums;

namespace OpenWorker.Commands;

public sealed partial class CommandManager
{
    /// <summary>
    /// A manager of the commands given, by their trigger with the slash.
    /// </summary>
    internal CommandManager(IReadOnlyDictionary<string, ICommand> commands)
    {
        Commands = commands;
    }

    /// <summary>
    /// Runs the command the message names when the account of the player has the level it needs. A command above the level
    /// is not run, as an unknown one: the original answers neither.
    /// </summary>
    public async ValueTask<bool> TryExecute(Entity player, string message, AccessLevel level)
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
        
        if (level < command.Required)
        {
            return false;
        }

        return await command.TryExecute(player, tokens[1..]);
    }
}