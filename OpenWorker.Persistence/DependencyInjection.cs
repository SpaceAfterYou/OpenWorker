using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenWorker.Domain.Stores;
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

    /// <summary>
    /// The stores the game logic sees through the interfaces of <c>OpenWorker.Domain.Stores</c>, over the context factory
    /// the server registers.
    /// </summary>
    public static IServiceCollection AddPersistenceStores(this IServiceCollection services)
    {
        services.TryAddSingleton<IPersonStore, PersonStore>();
        services.TryAddSingleton<IPostStore, PostStore>();
        services.TryAddSingleton<IPrologueStore, PrologueStore>();

        return services;
    }
}
