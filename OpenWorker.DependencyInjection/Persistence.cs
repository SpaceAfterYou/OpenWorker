using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenWorker.DependencyInjection.Health;
using OpenWorker.Extensions;
using OpenWorker.Persistence;

namespace OpenWorker.DependencyInjection;

public static class Persistence
{
    /// <summary>
    /// Registers the database and migrates it when the server starts.<br/>
    /// <c>ConnectionStrings:Postgres</c> points to the database; <c>Persistence:SensitiveDataLogging</c>
    /// writes the values of queries to the log, on by default in Debug builds only.
    /// The readiness of the server includes a connection to the database.
    /// </summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        return services
            .AddPersistenceOptions()
            .AddPostgresHealthCheck()
            .AddPersistenceBootstrapService()
            .AddPooledDbContextFactory<PersistenceContext>((provider, builder) =>
            {
                var options = provider.GetRequiredService<IOptions<PersistenceOptions>>().Value;

                builder
                    .UseLoggerFactory(provider.GetRequiredService<ILoggerFactory>())
                    .EnableDetailedErrors()
                    .EnableThreadSafetyChecks()
                    .EnableSensitiveDataLogging(options.SensitiveDataLogging)
                    .UseNpgsql(options.ConnectionString);
            });
    }
}
