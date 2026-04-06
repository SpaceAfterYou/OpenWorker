using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Domain.Batch.Enums;
using OpenWorker.Hotspot;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Maze.Responses;

namespace OpenWorker.Commands.SectorsCommands;

[CommandTrigger("sector", "clear")]
[CommandDescription("Clear maze sector")]
internal sealed partial class SectorClearCommand : ICommand
{
    [ExternalDependency]
    private World World { get; }
    
    [ExternalDependency]
    private BatchManager Provider { get; }
    
    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        var worldComponent = World.Get<WorldComponent>(player);
        
        if (!Provider.TryGetAndCache(worldComponent.Location, out var maze, out var type) || type != BatchType.Maze)
        {
            // TODO: Not in maze message
            return ValueTask.FromResult(false);
        }
        
        PrivateExecute(player);
        
        return ValueTask.FromResult(true);
    }
    
    private void PrivateExecute(Entity player)
    {
        var session = World.Get<ServerSessionComponent>(player);

        // TODO: resolve sector and success from maze state (command currently has no sector argument).
        session.Send(new MazeClearSectorResponse { Sector = 0, Status = true });
    }
}