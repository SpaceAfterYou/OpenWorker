using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Modules.SoulMetry;

namespace OpenWorker.Commands.SoulMetryCommands;

[CommandTrigger("soulmetry", "add")]
[CommandDescription("Start a soul metry as if its maze was cleared")]
internal sealed partial class SoulMetryAddCommand : ICommand
{
    [ExternalDependency]
    private SoulMetryManager Manager { get; }

    async ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!short.TryParse(tokens.ElementAtOrDefault(0), out var id))
        {
            return false;
        }

        return await Manager.AddAsync(player, id, CancellationToken.None);
    }
}
