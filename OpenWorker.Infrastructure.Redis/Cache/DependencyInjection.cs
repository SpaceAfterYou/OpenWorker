using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenWorker.Extensions;
using OpenWorker.Hotspot.Telemetry;
using StackExchange.Redis;

namespace OpenWorker.Hotspot.Cache;

public static class DependencyInjection
{
    public static void AddCacheBootstrapService(this IServiceCollection services)
    {
        services.AddHostedService<BootstrapService>();
    }

    /// <summary>
    /// The one connection to Redis of the process, at <c>ConnectionStrings:Redis</c>. The cache (Redis.OM), the presence store
    /// and the notifications between servers all use it, so that one profiler sees every command and
    /// <c>redis.call.duration</c> covers them all. A server that starts before Redis is ready keeps trying.
    /// </summary>
    public static IServiceCollection AddRedisConnection(this IServiceCollection services)
    {
        services.AddRedisOptions();
        services.AddRedisMetrics();

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IConnectionMultiplexer>(provider =>
        {
            var multiplexer = ConnectionMultiplexer.Connect(provider.GetRequiredService<IOptions<RedisOptions>>().Value.ToConfigurationOptions());

            provider.GetRequiredService<RedisMetrics>().Attach(multiplexer);

            return multiplexer;
        });

        services.TryAddSingleton<Reservations>();
        services.TryAddSingleton<LoginAttempts>();

        services.AddHostedService<RedisMetricsService>();

        return services;
    }
}
