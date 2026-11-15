using Arch.Core;
using Microsoft.Extensions.Options;
using OpenWorker.Channel;
using OpenWorker.Extensions;
using OpenWorker.Lua;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// Who sees whom in an open maze of the world, which holds up to fifty players: a grid of sight for each instance, as a channel
/// of a district has. A player is placed in it when its client has loaded the maze, sees and is seen by those of its cell and
/// the eight around, and is told who comes and goes as it crosses a cell. Everything runs on the world loop.
/// </summary>
public sealed class WorldMazeSight(World world, IOptions<WorldOptions>? sight = null)
{
    private readonly Dictionary<LuaMaze, ServiceChannel> _grids = [];

    private float CellSize { get; } = sight?.Value.VisibilityRadius ?? WorldOptions.DefaultVisibilityRadius;

    /// <summary>
    /// The grid the player stands in, when it plays an open maze of the world and has been placed in it.
    /// </summary>
    public ServiceChannel? Of(Entity player)
    {
        return world.IsAlive(player) && world.TryGet(player, out LuaMaze? maze) && maze is { IsWorld: true } &&
               _grids.TryGetValue(maze, out var grid) && grid.Contains(player)
            ? grid
            : null;
    }

    /// <summary>
    /// The client of the player has loaded the maze: those around see it come, and it sees them.
    /// </summary>
    public void Arrive(Entity player, LuaMaze maze)
    {
        if (!maze.IsWorld || !world.IsAlive(player))
        {
            return;
        }

        if (!_grids.TryGetValue(maze, out var grid))
        {
            grid = new ServiceChannel(CellSize);
            _grids.Add(maze, grid);
        }

        if (grid.Contains(player))
        {
            return;
        }

        grid.Join(player, world);
        grid.ShowAround(player, world);
    }

    /// <summary>
    /// The player leaves the maze: those who saw it are told it is gone. The grid goes with its last player.
    /// </summary>
    public void Leave(Entity player, LuaMaze maze)
    {
        if (!_grids.TryGetValue(maze, out var grid))
        {
            return;
        }

        grid.Leave(player);

        if (grid.Online == 0)
        {
            _grids.Remove(maze);
        }
    }
}
