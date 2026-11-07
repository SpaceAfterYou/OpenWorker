using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Modules.SoulMetry;

namespace OpenWorker.Commands.SoulMetryCommands;

[CommandTrigger("soulmetry", "collect")]
[CommandDescription("Find an object of a soul metry: the soul metry and the object")]
internal sealed partial class SoulMetryCollectCommand : ICommand
{
    [ExternalDependency]
    private SoulMetryManager Manager { get; }

    async ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!short.TryParse(tokens.ElementAtOrDefault(0), out var id) ||
            !int.TryParse(tokens.ElementAtOrDefault(1), out var obj))
        {
            return false;
        }

        return await Manager.CollectAsync(player, id, obj, CancellationToken.None);
    }
}
