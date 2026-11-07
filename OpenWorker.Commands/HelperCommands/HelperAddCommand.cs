using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Modules.Helpers;

namespace OpenWorker.Commands.HelperCommands;

[CommandTrigger("helper", "add")]
[CommandDescription("Give the player a helper")]
internal sealed partial class HelperAddCommand : ICommand
{
    [ExternalDependency]
    private HelperManager Manager { get; }

    async ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!int.TryParse(tokens.ElementAtOrDefault(0), out var helper))
        {
            return false;
        }

        return await Manager.AddAsync(player, helper, CancellationToken.None);
    }
}
