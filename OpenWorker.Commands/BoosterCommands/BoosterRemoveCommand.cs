using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Boosters.Responses;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Commands.BoosterCommands;

[CommandTrigger("booster", "remove")]
[CommandDescription("Remove player booster")]
internal sealed partial class BoosterRemoveCommand : ICommand
{
    [ExternalDependency]
    private World World { get; }

    [ExternalDependency]
    private ReadOnlyCollection<BoosterRow> Boosters { get; }

    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!short.TryParse(tokens.ElementAtOrDefault(0), out var id))
        {
            // TODO: Failed parser message
            return ValueTask.FromResult(false);
        }

        if (Boosters.Any(e => e.Id != id))
        {
            // TODO: Not found message
            return ValueTask.FromResult(false);
        }

        PrivateExecute(player, id);

        return ValueTask.FromResult(true);
    }

    private void PrivateExecute(Entity player, short id)
    {
        var session = World.Get<ServerSessionComponent>(player);

        session.Send(new BoosterRemoveResponse { Id = id });
    }
}
