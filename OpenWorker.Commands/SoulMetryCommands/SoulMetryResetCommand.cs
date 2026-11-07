using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Modules.SoulMetry;

namespace OpenWorker.Commands.SoulMetryCommands;

[CommandTrigger("soulmetry", "reset")]
[CommandDescription("Forget all soul metries of the player")]
internal sealed partial class SoulMetryResetCommand : ICommand
{
    [ExternalDependency]
    private SoulMetryManager Manager { get; }

    async ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        await Manager.ResetAsync(player, CancellationToken.None);

        return true;
    }
}
