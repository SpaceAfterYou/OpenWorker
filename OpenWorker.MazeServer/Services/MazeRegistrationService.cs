using Arch.Core;
using Microsoft.Extensions.Options;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache.Types;
using Redis.OM.Searching;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// Tells the other servers that this maze server runs, where clients reach it and how busy it is,
/// so that players are sent to the least busy one. Several maze servers of a gate share the load this way.
/// When the server drains the record is removed first, and no player is sent to a maze server that is going away.
/// </summary>
public sealed partial class MazeRegistrationService(
    IRedisCollection<MazeCache> mazes,
    WorldLoop loop,
    World world,
    IOptions<InstanceOptions> instance,
    IOptions<GateOptions> gate,
    IOptions<MazeEndpointOptions> maze,
    ILogger<MazeRegistrationService> logger
) :
    RegistrationService
{
    public static TimeSpan RenewInterval { get; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Three missed renewals and the server is taken for gone.
    /// </summary>
    public static TimeSpan Lifetime { get; } = RenewInterval * 3;

    private static QueryDescription Players { get; } = new QueryDescription().WithAll<ServerSessionComponent>();

    private Guid Instance { get; } = instance.Value.Identifier;
    private short Gate { get; } = gate.Value.Id;
    private (string Host, short Port) Address { get; } = (instance.Value.PublicHost, (short)(instance.Value.PublicPort ?? maze.Value.Port));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogServer(Instance, Gate, Address.Host, Address.Port);

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
                LogRenewFailed(e);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    protected override async Task RemoveAsync(CancellationToken cancellationToken)
    {
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
            LogRemoveFailed(e);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Maze server {Instance} of gate {Gate} at {Host}:{Port}.")]
    private partial void LogServer(Guid instance, short gate, string host, short port);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to renew the maze server record.")]
    private partial void LogRenewFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Failed to remove the maze server record.")]
    private partial void LogRemoveFailed(Exception exception);
}
