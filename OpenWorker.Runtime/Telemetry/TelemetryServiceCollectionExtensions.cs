using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OpenWorker.Hotspot.Telemetry;

public static class TelemetryServiceCollectionExtensions
{
    /// <summary>
    /// The classes that record the metrics of the runtime (world loop, handlers, simulation, sessions, movement), all on the
    /// <see cref="OpenWorkerMeter.Name"/> meter of the container's <see cref="System.Diagnostics.Metrics.IMeterFactory"/>. Recording
    /// costs next to nothing while no one listens. Every part of a server that measures asks for the ones it uses, so this may be
    /// called any number of times. Redis and Postgres add theirs on the same meter: <c>AddRedisMetrics</c> of
    /// <c>OpenWorker.Infrastructure.Redis</c>, <c>AddPostgresMetrics</c> of <c>OpenWorker.Persistence</c>.<br/>
    /// Exporting them is a separate matter: see <c>AddTelemetry</c> in <c>OpenWorker.DependencyInjection</c>.
    /// </summary>
    public static IServiceCollection AddOpenWorkerMetrics(this IServiceCollection services)
    {
        services.AddMetrics();

        services.TryAddSingleton<WorldLoopMetrics>();
        services.TryAddSingleton<HandlerMetrics>();
        services.TryAddSingleton<SimulationMetrics>();
        services.TryAddSingleton<SessionMetrics>();
        services.TryAddSingleton<MovementMetrics>();

        return services;
    }
}
