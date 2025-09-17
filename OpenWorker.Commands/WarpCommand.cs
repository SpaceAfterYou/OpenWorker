using Arch.Core;
using OpenWorker.Channel;
using OpenWorker.Hotspot.Commands;
using OpenWorker.Hotspot.Commands.Attributes;

namespace OpenWorker.Commands;

[StuffCommand("warp")]
public sealed class WarpCommand(World world, WorldManager worldManager) : AStuffCommand(world)
{
    protected override string GetTutorialMessage() => "warp (location id)";

    public override async ValueTask<bool> TryExecute(Entity player, IReadOnlyList<string> tokens)
    {
        if (!short.TryParse(tokens.ElementAtOrDefault(0), out var id))
        {
            SendTutorial(player, nameof(id));
            return false;
        }

        await worldManager
            .TryEnter(player, id)
            .ConfigureAwait(false);
        
        return true;
    }
}