using Microsoft.Extensions.DependencyInjection;

namespace OpenWorker.Hotspot.Cache;

public static class DependencyInjection
{
    public static void AddCacheBootstrapService(this IServiceCollection services)
    {
        services.AddHostedService<BootstrapService>();
    }
}