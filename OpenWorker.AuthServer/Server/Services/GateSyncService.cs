using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Modules.Login.Types;
using OpenWorker.Persistence;
using Redis.OM;
using Redis.OM.Searching;

namespace OpenWorker.AuthServer.Server.Services;

/// <summary>
/// Keeps the list of gates the login server shows: the names from the database, the address and the load from the
/// records the gates renew in Redis. A gate whose record expired (it stopped or crashed) is shown offline.
/// </summary>
public sealed partial class GateSyncService(IDbContextFactory<PersistenceContext> factory, IRedisCollection<GateCache> gateCache, List<GateInfo> registry, ILogger<GateSyncService> logger) : BackgroundService
{
    /// <summary>
    /// The list changes when a gate starts or stops, and its load is a hint; a round every 15 s is two small queries.
    /// </summary>
    public static TimeSpan Frequency { get; } = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Frequency);

        try
        {
            do
            {
                try
                {
                    await SyncAsync(stoppingToken);
                }

                catch (Exception e) when (e is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
                {
                    LogSyncFailed(logger, e);
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        catch (OperationCanceledException)
        {
            // The server is stopping.
        }
    }

    private async Task SyncAsync(CancellationToken cancellationToken)
    {
        // A context of its own for every round: one that failed in the middle is not used again.
        await using var database = await factory
            .CreateDbContextAsync(cancellationToken);

        var updatedAt = DateTime.UtcNow.AddMinutes(-1);

        var cacheList = await AsyncEnumerable.ToArrayAsync(gateCache.Where(e => e.UpdatedAt > updatedAt), cancellationToken);

        // The gates only: their persons are not part of the list.
        var persistentList = await database.Gates
            .ToArrayAsync(cancellationToken);

        foreach (var persistent in persistentList)
        {
            var gate = GetGateInfo(persistent, cacheList);

            var index = registry.FindIndex(e => e.Id == persistent.Id);

            if (index == -1)
            {
                LogNotInList(logger, persistent.Id);

                registry.Add(gate);

                LogAdded(logger, persistent.Id);

                continue;
            }

            registry[index] = gate;
        }
    }

    private GateInfo GetGateInfo(GatePersistent persistent, IReadOnlyCollection<GateCache> cacheList)
    {
        var cache = cacheList.FirstOrDefault(e => e.Identifier == persistent.Id);

        if (cache is not null)
        {
            return new GateInfo
            {
                Id = persistent.Id,
                Name = persistent.Name,
                Workload = cache.Workload,
                Address = cache.Host,
                Port = cache.Port,
                OnlineCount = cache.OnlineCount,
            };
        }

        LogNotInCache(logger, persistent.Id);
        
        return new GateInfo
        {
            Id = persistent.Id,
            Name = persistent.Name,
            Workload = GateWorkload.Offline,
            Address = string.Empty,
            Port = 0,
            OnlineCount = 0,
        };
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to synchronize the gates.")]
    private static partial void LogSyncFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Gate not found in list: {Gate}")]
    private static partial void LogNotInList(ILogger logger, int gate);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Gate added: {Gate}")]
    private static partial void LogAdded(ILogger logger, int gate);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Gate not found in cache: {Gate}")]
    private static partial void LogNotInCache(ILogger logger, int gate);
}
