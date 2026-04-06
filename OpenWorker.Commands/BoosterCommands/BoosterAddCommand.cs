using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Boosters.Enums;
using OpenWorker.Hotspot.Modules.Boosters.Responses;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Commands.BoosterCommands;

[CommandTrigger("booster", "add")]
[CommandDescription("Add player booster")]
internal sealed partial class BoosterAddCommand : ICommand
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

        if (Boosters.All(e => e.Id != id))
        {
            // TODO: Not found message
            return ValueTask.FromResult(false);
        }

        if (!Enum.TryParse(tokens.ElementAtOrDefault(1), out BoosterConsumeArea area))
        {
            // TODO: Failed parser message
            return ValueTask.FromResult(false);
        }

        PrivateExecute(player, id, area);

        return ValueTask.FromResult(true);
    }

    private void PrivateExecute(Entity player, short id, BoosterConsumeArea area)
    {
        var now = DateTimeOffset.UtcNow;
        var remaining = now.AddMinutes(15) - now;

        var session = World.Get<ServerSessionComponent>(player);

        session.Send(new BoosterAddResponse
        {
            Identifier = id,
            Remaining = remaining,
            Area = area
        });
    }
}
