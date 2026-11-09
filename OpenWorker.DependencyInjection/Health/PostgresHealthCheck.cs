using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OpenWorker.Extensions;
using OpenWorker.Persistence;

namespace OpenWorker.DependencyInjection.Health;

/// <summary>
/// The database accepts a connection within <see cref="HealthOptions.CheckTimeout"/>. The context comes from the
/// factory, lives for the check and goes back to the pool; the connection is one of the pool, and the query is the
/// provider's own existence probe, so the check costs the database next to nothing.
/// </summary>
internal sealed class PostgresHealthCheck(IDbContextFactory<PersistenceContext> factory, IOptions<HealthOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var timeout = options.Value.CheckTimeout;

        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        limit.CancelAfter(timeout);

        try
        {
            await using var database = await factory.CreateDbContextAsync(limit.Token);

            return await database.Database.CanConnectAsync(limit.Token)
                ? HealthCheckResult.Healthy("The database accepts connections.")
                : HealthCheckResult.Unhealthy("The database does not accept connections.");
        }

        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy($"The database did not answer within {timeout}.");
        }

        catch (Exception e) when (e is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("The database does not answer.", e);
        }
    }
}
