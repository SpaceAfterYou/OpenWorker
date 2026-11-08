using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace OpenWorker.Persistence;

public static class DependencyInjection
{
    /// <summary>
    /// Hosted services start one after another in the order they are registered, so the migration
    /// goes first: the schema has to exist before any other service starts.
    /// </summary>
    public static IServiceCollection AddPersistenceBootstrapService(this IServiceCollection services)
    {
        if (services.Any(e => e.ImplementationType == typeof(PersistenceBootstrapService)))
        {
            return services;
        }

        services.Insert(0, ServiceDescriptor.Singleton<IHostedService, PersistenceBootstrapService>());

        return services;
    }
}
