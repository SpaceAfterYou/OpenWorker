using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OpenWorker.Hotspot.Telemetry;

public static class RedisTelemetryServiceCollectionExtensions
{
    /// <summary>
    /// The metrics of the calls to Redis (<see cref="RedisMetrics"/>), next to those of the runtime on the same meter. May be
    /// called any number of times.
    /// </summary>
    public static IServiceCollection AddRedisMetrics(this IServiceCollection services)
    {
        services.AddOpenWorkerMetrics();
        services.TryAddSingleton<RedisMetrics>();

        return services;
    }
}
