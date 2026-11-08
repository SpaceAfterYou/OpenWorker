using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Modules.Login.Types;
using OpenWorker.Persistence;
using Redis.OM;
using Redis.OM.Searching;

namespace OpenWorker.AuthServer.Server.Services;

public sealed partial class GateSyncService(IDbContextFactory<PersistenceContext> factory, IRedisCollection<GateCache> gateCache, List<GateInfo> registry, ILogger<GateSyncService> logger) : BackgroundService
{
    // TODO: 15sec
    private static TimeSpan Frequency { get; } = TimeSpan.FromSeconds(1);

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

        var persistentList = await database.Gates
            .Include(e => e.Persons)
            .ToArrayAsync(cancellationToken);

        foreach (var persistent in persistentList)
        {
            var gate = GetGateInfo(persistent, cacheList);

            var index = registry.FindIndex(e => e.Id == persistent.Id);

            if (index == -1)
            {
                logger.LogWarning("Gate not found in list: {Gate}", persistent.Id);

                registry.Add(gate);

                logger.LogDebug("Gate added: {Gate}", persistent.Id);

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

        logger.LogWarning("Gate not found in cache: {Gate}", persistent.Id);
        
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
}
