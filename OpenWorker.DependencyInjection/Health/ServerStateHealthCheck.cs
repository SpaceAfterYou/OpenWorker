using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OpenWorker.DependencyInjection.Health;

/// <summary>
/// The server takes players: its game listener has started and it is not stopping. A stopping server is alive, so
/// this belongs to readiness only: the orchestrator stops sending players, and does not restart a server that drains.
/// </summary>
internal sealed class ServerStateHealthCheck(ServerState state) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var result = state switch
        {
            { IsDraining: true } => HealthCheckResult.Unhealthy("The server is stopping and takes no new players."),
            { IsListening: false } => HealthCheckResult.Unhealthy("The game listener has not started."),
            _ => HealthCheckResult.Healthy("The game listener accepts connections.")
        };

        return Task.FromResult(result);
    }
}
