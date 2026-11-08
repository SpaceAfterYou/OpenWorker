using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenWorker.Persistence;

namespace OpenWorker.DependencyInjection;

public static class Persistence
{
#if DEBUG
    private const bool SensitiveDataLoggingByDefault = true;
#else
    private const bool SensitiveDataLoggingByDefault = false;
#endif

    /// <summary>
    /// Registers the database and migrates it when the server starts.<br/>
    /// <c>ConnectionStrings:Postgres</c> points to the database; <c>Persistence:SensitiveDataLogging</c>
    /// writes the values of queries to the log, on by default in Debug builds only.
    /// </summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        return services
            .AddPersistenceBootstrapService()
            .AddPooledDbContextFactory<PersistenceContext>((provider, builder) =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();

                builder
                    .UseLoggerFactory(provider.GetRequiredService<ILoggerFactory>())
                    .EnableDetailedErrors()
                    .EnableThreadSafetyChecks()
                    .EnableSensitiveDataLogging(configuration.GetValue("Persistence:SensitiveDataLogging", SensitiveDataLoggingByDefault))
                    .UseNpgsql(PersistenceConnection.Resolve(configuration));
            });
    }
}
