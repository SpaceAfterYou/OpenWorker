using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Modeling;

namespace OpenWorker.Hotspot.Cache;

internal sealed class BootstrapService(
    IRedisConnectionProvider provider,
    ILogger<BootstrapService> logger
) :
    BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogDebug("Bootstrapping cache...");

        var types = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(x => x.GetCustomAttribute<DocumentAttribute>() is not null)
            .ToArray();

        await Task.WhenAll(types.Select(async type =>
        {
            logger.LogDebug("Drop index for type: {Name}", type.Name);

            await Task
                .Run(() => provider.Connection.DropIndexAndAssociatedRecords(type), stoppingToken)
                .ConfigureAwait(false);
        })).ConfigureAwait(false);

        await Task.WhenAll(types.Select(async x =>
        {
            logger.LogDebug("Create index for type: {Name}", x.Name);

            await provider.Connection
                .CreateIndexAsync(x)
                .ConfigureAwait(false);
        })).ConfigureAwait(false);

        logger.LogDebug("Cache bootstrapped.");
    }
}