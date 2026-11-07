using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Modules.Boosters;

namespace OpenWorker.Commands.BoosterCommands;

[CommandTrigger("booster", "remove")]
[CommandDescription("Take a booster from the player")]
internal sealed partial class BoosterRemoveCommand : ICommand
{
    [ExternalDependency]
    private BoosterManager Manager { get; }

    async ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!short.TryParse(tokens.ElementAtOrDefault(0), out var booster))
        {
            return false;
        }

        return await Manager.RemoveAsync(player, booster, CancellationToken.None);
    }
}
