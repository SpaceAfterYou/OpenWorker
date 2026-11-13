using Microsoft.Extensions.Options;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Lua;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// The records of the open mazes of the world this server runs (<see cref="WorldMazeDirectory"/>): each instance renews the
/// record of its channel with the players it has, on the world loop, and gives it back when it ends, so the next player of the
/// channel opens a new one.
/// </summary>
public sealed partial class WorldMazeKeeper(
    WorldMazeDirectory directory,
    IOptions<GateOptions> gate,
    IOptions<InstanceOptions> instance,
    IOptions<MazeEndpointOptions> endpoint,
    ILogger<WorldMazeKeeper> logger)
{
    private short Gate { get; } = gate.Value.Id;

    /// <summary>
    /// This server as the record names it.
    /// </summary>
    private (Guid Server, string Host, short Port) Server { get; } =
        (instance.Value.Identifier, instance.Value.PublicHost, (short)(instance.Value.PublicPort ?? endpoint.Value.Port));

    /// <summary>
    /// Renews the records of the instances that run. An instance whose record names another one keeps its players; new ones go
    /// to the other.
    /// </summary>
    public async ValueTask KeepAllAsync(IEnumerable<LuaMaze> running)
    {
        foreach (var maze in running.Where(e => e is { IsWorld: true, IsClosed: false }).ToArray())
        {
            try
            {
                await directory.HoldAsync(Gate, maze.Maze, maze.World, maze.Instance, Server, maze.MemberCount);
            }

            catch (Exception e) when (e is not OperationCanceledException)
            {
                LogKeepFailed(e, maze.Maze, maze.World);
            }
        }
    }

    /// <summary>
    /// The instance ended: its record goes, if it still names it. A record that cannot be removed expires by itself.
    /// </summary>
    public async ValueTask ReleaseAsync(LuaMaze maze)
    {
        if (!maze.IsWorld)
        {
            return;
        }

        try
        {
            await directory.ReleaseAsync(Gate, maze.Maze, maze.World, maze.Instance);
        }

        catch (Exception e) when (e is not OperationCanceledException)
        {
            LogKeepFailed(e, maze.Maze, maze.World);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The record of the open maze {Maze} on channel {Channel} could not be written, it expires by itself")]
    private partial void LogKeepFailed(Exception exception, short maze, byte channel);
}

/// <summary>
/// Renews the records of the open mazes of the world on a timer, on the world loop.
/// </summary>
public sealed partial class WorldMazeRenewal(WorldMazeKeeper keeper, MazeInstances instances, WorldLoop loop, ILogger<WorldMazeRenewal> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(WorldMazeDirectory.RenewInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await loop.InvokeAsync(() => keeper.KeepAllAsync(instances.Running));
                }

                catch (Exception e) when (e is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
                {
                    LogRenewFailed(e);
                }
            }
        }

        catch (OperationCanceledException)
        {
            // The server is stopping.
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "The records of the open mazes could not be renewed")]
    private partial void LogRenewFailed(Exception exception);
}
