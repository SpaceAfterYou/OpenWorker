using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Modules.Boosters;

namespace OpenWorker.Commands.BoosterCommands;

[CommandTrigger("booster", "clear")]
[CommandDescription("Take all boosters from the player")]
internal sealed partial class BoosterClearCommand : ICommand
{
    [ExternalDependency]
    private BoosterManager Manager { get; }

    async ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        await Manager.ClearAsync(player, CancellationToken.None);

        return true;
    }
}
