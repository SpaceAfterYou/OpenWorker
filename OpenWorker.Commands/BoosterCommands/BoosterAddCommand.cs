using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Modules.Boosters;

namespace OpenWorker.Commands.BoosterCommands;

[CommandTrigger("booster", "add")]
[CommandDescription("Give the player a booster: the booster and, optionally, how many seconds it lasts")]
internal sealed partial class BoosterAddCommand : ICommand
{
    [ExternalDependency]
    private BoosterManager Manager { get; }

    async ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!short.TryParse(tokens.ElementAtOrDefault(0), out var booster))
        {
            return false;
        }

        TimeSpan? duration = int.TryParse(tokens.ElementAtOrDefault(1), out var seconds) ? TimeSpan.FromSeconds(seconds) : null;

        return await Manager.AddAsync(player, booster, duration, CancellationToken.None);
    }
}
