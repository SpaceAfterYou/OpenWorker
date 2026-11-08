using Arch.Core;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache.Types;
using Redis.OM.Searching;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// Tells the other servers that this maze server runs, where clients reach it and how busy it is,
/// so that players are sent to the least busy one. Several maze servers of a gate share the load this way.
/// </summary>
public sealed class MazeRegistrationService(
    IRedisCollection<MazeCache> mazes,
    WorldLoop loop,
    World world,
    IConfiguration configuration,
    ILogger<MazeRegistrationService> logger
) :
    BackgroundService
{
    public static TimeSpan RenewInterval { get; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Three missed renewals and the server is taken for gone.
    /// </summary>
    public static TimeSpan Lifetime { get; } = RenewInterval * 3;

    private static QueryDescription Players { get; } = new QueryDescription().WithAll<ServerSessionComponent>();

    private Guid Instance { get; } = configuration.GetInstance();
    private short Gate { get; } = configuration.GetGate();
    private (string Host, short Port) Address { get; } = ResolveAddress(configuration);

    private static (string Host, short Port) ResolveAddress(IConfiguration configuration)
    {
        var (host, port) = configuration.GetPublicAddress();

        return (host, port ?? configuration.GetMazeAddress().Port);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Maze server {Instance} of gate {Gate} at {Host}:{Port}.", Instance, Gate, Address.Host, Address.Port);

        using var timer = new PeriodicTimer(RenewInterval);

        do
        {
            try
            {
                var players = await loop.InvokeAsync(() => world.CountEntities(Players));

                await mazes.InsertAsync(new MazeCache
                {
                    Guid = Instance,
                    Gate = Gate,
                    Players = players,
                    Host = Address.Host,
                    Port = Address.Port
                }, Lifetime);
            }

            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogWarning(e, "Failed to renew the maze server record.");
            }
        }
        while (await timer.WaitForNextTickAsync(cancellationToken));
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        try
        {
            await mazes.DeleteAsync(new MazeCache
            {
                Guid = Instance,
                Gate = Gate,
                Players = 0,
                Host = Address.Host,
                Port = Address.Port
            });
        }

        catch (Exception e)
        {
            // The record expires by itself.
            logger.LogDebug(e, "Failed to remove the maze server record.");
        }
    }
}
