using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;

namespace OpenWorker.Gameplay.Modules.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// The write-behind of a game server: the snapshots, the writer, the periodic flush and the flush of one player
    /// (on a move to another server and when its connection closes). The writer is registered first, so the host stops
    /// it last, after the last round of the flush.
    /// </summary>
    public static IServiceCollection AddPersonPersistence(this IServiceCollection services)
    {
        if (services.Any(e => e.ServiceType == typeof(PersistenceWriter)))
        {
            return services;
        }

        services.AddPersistenceOptions();
        services.TryAddSingleton(TimeProvider.System);

        services.AddSingleton<PersonSnapshots>();
        services.AddSingleton<PersistenceWriter>();
        services.AddHostedService(provider => provider.GetRequiredService<PersistenceWriter>());

        services.AddSingleton<PersistenceFlushService>();
        services.AddHostedService(provider => provider.GetRequiredService<PersistenceFlushService>());

        services.AddSingleton<PersonFlush>();
        services.Replace(ServiceDescriptor.Singleton<IPersonFlush>(provider => provider.GetRequiredService<PersonFlush>()));
        services.AddSingleton<ISessionCloseHandler>(provider => provider.GetRequiredService<PersonFlush>());

        return services;
    }
}
