using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Hotspot.Commands.Attributes;
using OpenWorker.Hotspot.Modules.SoulMetry.Responses;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Commands;

[StuffCommand("soulmetry", "update")]
public sealed class SoulMetryUpdateCommand(World world, ReadOnlyCollection<SoulMetryRow> soulMetryCollection) : AStuffCommand(world)
{
    protected override string GetTutorialMessage() => "soulmetry-update (id) (index) (is completed)";

    public override ValueTask<bool> TryExecute(Entity player, IReadOnlyList<string> tokens)
    {
        var session = World.Get<ServerSessionComponent>(player);
        var actor = World.Get<ActorComponent>(player);

        if (int.TryParse(tokens.ElementAtOrDefault(0), out var id) is false)
        {
            SendTutorial(player, nameof(id));
            return ValueTask.FromResult(false);
        }

        if (soulMetryCollection.Any(e => e.Id == id) is false)
        {
            SendTutorial(player, nameof(id));
            return ValueTask.FromResult(false);
        }

        if (short.TryParse(tokens.ElementAtOrDefault(1), out var index) is false)
        {
            SendTutorial(player, nameof(index));
            return ValueTask.FromResult(false);
        }

        if (bool.TryParse(tokens.ElementAtOrDefault(2), out var isCompleted) is false)
        {
            SendTutorial(player, nameof(isCompleted));
            return ValueTask.FromResult(false);
        }

        var value = SoulMetry.ChangeEpisode(0, index, isCompleted);
        session.Send(new SoulMetryUpdateResponse(id, value));
        return ValueTask.FromResult(true);
    }
}