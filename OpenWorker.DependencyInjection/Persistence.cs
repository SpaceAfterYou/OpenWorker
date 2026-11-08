using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenWorker.Extensions;
using OpenWorker.Hotspot.Telemetry;
using OpenWorker.Persistence;

namespace OpenWorker.DependencyInjection;

public static class Persistence
{
    /// <summary>
    /// Registers the database and migrates it when the server starts.<br/>
    /// <c>ConnectionStrings:Postgres</c> points to the database; <c>Persistence:SensitiveDataLogging</c>
    /// writes the values of queries to the log, on by default in Debug builds only. The commands are timed for
    /// <c>postgres.call.duration</c>.
    /// </summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        return services
            .AddPersistenceOptions()
            .AddOpenWorkerMetrics()
            .AddPersistenceBootstrapService()
            .AddPooledDbContextFactory<PersistenceContext>((provider, builder) =>
            {
                var options = provider.GetRequiredService<IOptions<PersistenceOptions>>().Value;

                builder
                    .UseLoggerFactory(provider.GetRequiredService<ILoggerFactory>())
                    .EnableDetailedErrors()
                    .EnableThreadSafetyChecks()
                    .EnableSensitiveDataLogging(options.SensitiveDataLogging)
                    .AddInterceptors(provider.GetRequiredService<PostgresCommandInterceptor>())
                    .UseNpgsql(options.ConnectionString);
            });
    }
}
