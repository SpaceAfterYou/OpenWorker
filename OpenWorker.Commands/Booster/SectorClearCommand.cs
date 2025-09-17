using Arch.Core;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Commands;
using OpenWorker.Hotspot.Commands.Attributes;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Maze.Responses;

namespace OpenWorker.Commands.Booster;

[StuffCommand("sector", "clear")]
public sealed class SectorClearCommand(BatchManager provider, World world) : AStuffCommand(world)
{
    protected override string GetTutorialMessage() => "sector-clear";

    public override ValueTask<bool> TryExecute(Entity player, IReadOnlyList<string> tokens)
    {
        var worldComponent = World.Get<WorldComponent>(player);
        
        if (!provider.TryGetAndCache(worldComponent, out var maze, out var type) || type != BatchType.Maze)
        {
            return ValueTask.FromResult(false);
        }
        
        var session = World.Get<ServerSessionComponent>(player);

        session.Send(new MazeClearSectorResponse());
        return ValueTask.FromResult(true);
    }
}