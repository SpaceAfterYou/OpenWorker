using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Commands;
using OpenWorker.Hotspot.Commands.Attributes;
using OpenWorker.Hotspot.Modules.Boosters.Responses;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Commands.Booster;

[StuffCommand("booster", "clear")]
public sealed class BoosterClearCommand(World world, ReadOnlyCollection<BoosterRow> boosters) : AStuffCommand(world)
{
    protected override string GetTutorialMessage() => "booster-clear";

    public override ValueTask<bool> TryExecute(Entity player, IReadOnlyList<string> tokens)
    {
        var session = World.Get<ServerSessionComponent>(player);

        session.Send(new BoosterClearResponse());
        return ValueTask.FromResult(true);
    }
}