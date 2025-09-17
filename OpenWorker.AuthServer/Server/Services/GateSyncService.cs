using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Modules.Login.Types;
using OpenWorker.Persistence;
using Redis.OM;
using Redis.OM.Searching;

namespace OpenWorker.AuthServer.Server.Services;

public sealed class GateSyncService(IDbContextFactory<PersistenceContext> factory, IRedisCollection<GateCache> gateCache, List<GateInfo> registry, ILogger<GateSyncService> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _ = Task.Factory.StartNew(
            () => OnTickAsync(cancellationToken).ConfigureAwait(false),
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default
        );
        
        return Task.CompletedTask;
    }

    private async Task OnTickAsync(CancellationToken cancellationToken)
    {
        await using var database = await factory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        
        while (cancellationToken.IsCancellationRequested is false)
        {
            var updatedAt = DateTime.UtcNow.AddMinutes(-1);
            
            var cacheList = await gateCache.Where(e => e.UpdatedAt > updatedAt)
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);
            
            var persistentList = await database.Gates
                .Include(e => e.Persons)
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var persistent in persistentList)
            {
                var gate = GetGateInfo(persistent, cacheList);
                
                var index = registry.FindIndex(e => e.Id == persistent.Id);
                
                if (index == -1)
                {
                    logger.LogWarning("Gate not found in list: {}", persistent.Id);
                    
                    registry.Add(gate);
                    
                    logger.LogDebug("Gate added: {}", persistent.Id);
                    
                    continue;
                }

                registry[index] = gate;
            }

            // TODO: 15sec
            await Task
                .Delay(TimeSpan.FromSeconds(1), cancellationToken)
                .ConfigureAwait(false);
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

        logger.LogWarning("Gate not found in cache: {}", persistent.Id);
        
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
}