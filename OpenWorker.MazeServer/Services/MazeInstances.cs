using Arch.Core;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Lua;
using Redis.OM.Searching;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// The mazes this server runs, by instance. A player alone has an instance of their own; the members of a party share
/// one, which the first of them to arrive opens and the others join where it stands. A member who leaves leaves the
/// instance, the maze goes on for the rest, and the last one out ends it. Everything runs on the world loop.
/// </summary>
public sealed partial class MazeInstances(World world, IRedisCollection<PartyMazeCache> parties, ILogger<MazeInstances> logger)
{
    private readonly Dictionary<Guid, LuaMaze> _running = [];

    /// <summary>
    /// Instances that are being opened: the script and the navigation mesh are read before the maze exists, and a member
    /// who arrives meanwhile waits for the same maze instead of opening a second one.
    /// </summary>
    private readonly Dictionary<Guid, Task<LuaMaze?>> _opening = [];

    /// <summary>
    /// How many instances run.
    /// </summary>
    public int Count => _running.Count;

    /// <summary>
    /// The instance, opened by <paramref name="open"/> when it does not run yet. Null when it cannot be opened.
    /// </summary>
    public async ValueTask<LuaMaze?> GetOrOpenAsync(Guid instance, Func<ValueTask<LuaMaze?>> open)
    {
        if (_running.TryGetValue(instance, out var running))
        {
            if (!running.IsClosed)
            {
                return running;
            }

            // A maze whose script failed is over; the member who comes now opens a new one.
            _running.Remove(instance);
        }

        if (_opening.TryGetValue(instance, out var pending))
        {
            return await pending;
        }

        var opening = open().AsTask();

        _opening[instance] = opening;

        LuaMaze? maze;

        try
        {
            maze = await opening;
        }

        finally
        {
            _opening.Remove(instance);
        }

        if (maze is not null)
        {
            _running[instance] = maze;
        }

        return maze;
    }

    /// <summary>
    /// The player has left the maze, by its exit, by leaving the game or by losing the connection. The maze goes on for the
    /// rest of the party; the last one out ends it, and the record of the party maze goes with it.
    /// </summary>
    public async ValueTask LeaveAsync(Entity player)
    {
        if (!world.IsAlive(player) || !world.TryGet(player, out LuaMaze? maze) || maze is null)
        {
            return;
        }

        world.Remove<LuaMaze>(player);

        if (maze.Leave(player))
        {
            await EndAsync(maze);
        }
    }

    /// <summary>
    /// A maze that was opened for a player who left before getting in, with nobody else in it, ends at once.
    /// </summary>
    public async ValueTask AbandonAsync(LuaMaze maze, Entity player)
    {
        if (maze.Leave(player) || maze.MemberCount == 0)
        {
            await EndAsync(maze);
        }
    }

    private async ValueTask EndAsync(LuaMaze maze)
    {
        if (_running.TryGetValue(maze.Instance, out var running) && ReferenceEquals(running, maze))
        {
            _running.Remove(maze.Instance);
        }

        if (!maze.IsClosed)
        {
            maze.Close();
        }

        if (maze.Party == 0)
        {
            return;
        }

        try
        {
            // Another instance of the party may have taken the record meanwhile: only this one's record goes.
            if (await parties.FindByIdAsync(maze.Party.ToString(System.Globalization.CultureInfo.InvariantCulture)) is { } record &&
                record.Instance == maze.Instance)
            {
                await parties.DeleteAsync(record);
            }
        }

        catch (Exception e) when (e is not OperationCanceledException)
        {
            // The record expires by itself.
            LogRecordNotRemoved(e, maze.Party);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The record of the maze of party {Party} could not be removed, it expires by itself")]
    private partial void LogRecordNotRemoved(Exception exception, int party);
}
