using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenWorker.Extensions;

namespace OpenWorker.DependencyInjection.Health;

/// <summary>
/// Puts the checks into the container. The module that owns a dependency registers the check for it
/// (<c>AddCache</c> the Redis one, <c>AddPersistence</c> the database one, <c>AddHotspot</c> the world loop and the
/// listener), so a server has exactly the checks of the parts it has. The endpoint that serves them is
/// <see cref="HealthHostingExtensions.UseHealthEndpoint"/>.
/// </summary>
internal static class HealthChecksExtensions
{
    /// <summary>
    /// Redis answers.
    /// </summary>
    public static IServiceCollection AddRedisHealthCheck(this IServiceCollection services)
    {
        return services.AddHealthCheckOnce<RedisHealthCheck>("redis", HealthTags.Ready);
    }

    /// <summary>
    /// The database accepts connections.
    /// </summary>
    public static IServiceCollection AddPostgresHealthCheck(this IServiceCollection services)
    {
        return services.AddHealthCheckOnce<PostgresHealthCheck>("postgres", HealthTags.Ready);
    }

    /// <summary>
    /// The world loop runs callbacks, and the listener is up and not draining.
    /// </summary>
    public static IServiceCollection AddServerHealthChecks(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        return services
            .AddHealthCheckOnce<WorldLoopHealthCheck>("world-loop", HealthTags.Live, HealthTags.Ready)
            .AddHealthCheckOnce<ServerStateHealthCheck>("listener", HealthTags.Ready);
    }

    /// <summary>
    /// Every module asks for its check, and a module may be added twice: the second time does nothing, because two
    /// checks of one name stop the health service.
    /// </summary>
    private static IServiceCollection AddHealthCheckOnce<TCheck>(this IServiceCollection services, string name, params string[] tags)
        where TCheck : class, IHealthCheck
    {
        if (services.Any(e => e.ServiceType == typeof(TCheck)))
        {
            return services;
        }

        services.AddHealthOptions();

        // A singleton: the check of the world loop remembers the callback it has posted.
        services.AddSingleton<TCheck>();
        services.AddHealthChecks().AddCheck<TCheck>(name, failureStatus: HealthStatus.Unhealthy, tags);

        return services;
    }
}
