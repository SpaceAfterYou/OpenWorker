using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Hotspot.Commands.Attributes;
using OpenWorker.Hotspot.Modules.Boosters.Responses;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Commands.Booster;

[StuffCommand("booster", "remove")]
public sealed class BoosterRemoveCommand(World world, ReadOnlyCollection<BoosterRow> boosters) : AStuffCommand(world)
{
    protected override string GetTutorialMessage() => "booster-remove (id)";

    public override ValueTask<bool> TryExecute(Entity player, IReadOnlyList<string> tokens)
    {
        var session = World.Get<ServerSessionComponent>(player);

        if (!short.TryParse(tokens.ElementAtOrDefault(0), out var id) || !boosters.Any(e => e.Id == id))
        {
            SendTutorial(player, nameof(id));
            return ValueTask.FromResult(false);
        }

        session.Send(new BoosterRemoveResponse(id));
        return ValueTask.FromResult(true);
    }
}