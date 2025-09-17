using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Hotspot.Commands.Attributes;
using OpenWorker.Hotspot.Modules.Boosters.Enums;
using OpenWorker.Hotspot.Modules.Boosters.Responses;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Commands.Booster;

[StuffCommand("booster", "add")]
public sealed class BoosterAddCommand(World world, ReadOnlyCollection<BoosterRow> boosters) : AStuffCommand(world)
{
    protected override string GetTutorialMessage() => "booster-add (id) (area)";

    public override ValueTask<bool> TryExecute(Entity player, IReadOnlyList<string> tokens)
    {
        var session = World.Get<ServerSessionComponent>(player);

        if (short.TryParse(tokens.ElementAtOrDefault(0), out var id) is false)
        {
            SendTutorial(player, nameof(id));
            return ValueTask.FromResult(false);
        }

        if (boosters.Any(e => e.Id == id) is false)
        {
            SendTutorial(player, nameof(id));
            return ValueTask.FromResult(false);
        }

        if (Enum.TryParse(tokens.ElementAtOrDefault(1), out BoosterConsumeArea area) is false)
        {
            SendTutorial(player, nameof(area));
            return ValueTask.FromResult(false);
        }

        var now = DateTimeOffset.UtcNow;
        var remaining = now.AddMinutes(15) - now;

        session.Send(new BoosterAddResponse(id, remaining, area));
        return ValueTask.FromResult(true);
    }
}