using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Boosters.Responses;

namespace OpenWorker.Commands.BoosterCommands;

[CommandTrigger("booster", "clear")]
[CommandDescription("Clear all player boosters")]
internal sealed partial class BoosterClearCommand : ICommand
{
    [ExternalDependency]
    private World World { get; }

    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        var session = World.Get<ServerSessionComponent>(player);

        session.Send(new BoosterClearResponse());

        return ValueTask.FromResult(true);
    }
}
