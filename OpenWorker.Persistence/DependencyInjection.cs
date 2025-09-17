using Microsoft.Extensions.DependencyInjection;

namespace OpenWorker.Persistence;

public static class DependencyInjection
{
    public static void AddPersistenceBootstrapService(this IServiceCollection services)
    {
        services.AddHostedService<PersistenceBootstrapService>();
    }
}