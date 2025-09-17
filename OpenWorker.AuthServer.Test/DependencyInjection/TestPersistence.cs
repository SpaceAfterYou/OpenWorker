using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenWorker.Persistence;

namespace OpenWorker.AuthServer.Test.DependencyInjection;

public static class TestPersistence
{
    public static IServiceCollection AddTestPersistence(this IServiceCollection services) => services
        .AddPooledDbContextFactory<PersistenceContext>(AddPooledDbContextFactory);

    private static void AddPooledDbContextFactory(IServiceProvider provider, DbContextOptionsBuilder builder) => builder
        .UseLoggerFactory(provider.GetRequiredService<ILoggerFactory>())
        .EnableDetailedErrors()
        .EnableThreadSafetyChecks()
        .EnableSensitiveDataLogging()
        .UseInMemoryDatabase(Guid.NewGuid().ToString());
}