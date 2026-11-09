using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OpenWorker.Hotspot.Telemetry;

public static class PostgresTelemetryServiceCollectionExtensions
{
    /// <summary>
    /// The metrics of the commands sent to Postgres (<see cref="PostgresMetrics"/>) and the interceptor of EF Core that feeds
    /// them, next to those of the runtime on the same meter. May be called any number of times.
    /// </summary>
    public static IServiceCollection AddPostgresMetrics(this IServiceCollection services)
    {
        services.AddOpenWorkerMetrics();
        services.TryAddSingleton<PostgresMetrics>();
        services.TryAddSingleton<PostgresCommandInterceptor>();

        return services;
    }
}
