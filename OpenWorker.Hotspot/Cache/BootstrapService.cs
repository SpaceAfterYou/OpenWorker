using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Modeling;

namespace OpenWorker.Hotspot.Cache;

internal sealed partial class BootstrapService(
    IRedisConnectionProvider provider,
    ILogger<BootstrapService> logger
) :
    BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogBootstrapping();

        var types = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(x => x.GetCustomAttribute<DocumentAttribute>() is not null)
            .ToArray();

        await Task.WhenAll(types.Select(async type =>
        {
            LogDropIndex(type.Name);

            await Task
                .Run(() => provider.Connection.DropIndexAndAssociatedRecords(type), stoppingToken);
        }));

        await Task.WhenAll(types.Select(async x =>
        {
            LogCreateIndex(x.Name);

            await provider.Connection
                .CreateIndexAsync(x);
        }));

        LogBootstrapped();
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Bootstrapping cache...")]
    private partial void LogBootstrapping();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Drop index for type: {Name}")]
    private partial void LogDropIndex(string name);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Create index for type: {Name}")]
    private partial void LogCreateIndex(string name);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache bootstrapped.")]
    private partial void LogBootstrapped();
}