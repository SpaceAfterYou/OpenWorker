using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenWorker.Persistence;

namespace OpenWorker.DependencyInjection;

public static class Persistence
{
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        return services.AddPooledDbContextFactory<PersistenceContext>((provider, builder) => builder
            .UseLoggerFactory(provider.GetRequiredService<ILoggerFactory>())
            .EnableDetailedErrors()
            .EnableThreadSafetyChecks()
            .EnableSensitiveDataLogging()
            .UseNpgsql("Host=postgres;Port=5432;Database=openworker;Username=postgres"));
    }
}