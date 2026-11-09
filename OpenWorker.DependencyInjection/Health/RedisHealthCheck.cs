using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OpenWorker.Extensions;
using Redis.OM.Contracts;

namespace OpenWorker.DependencyInjection.Health;

/// <summary>
/// Redis answers a <c>PING</c> within <see cref="HealthOptions.CheckTimeout"/>. One round trip of one short
/// command on the connection the cache already holds: the check costs Redis next to nothing.
/// </summary>
internal sealed class RedisHealthCheck(IRedisConnectionProvider redis, IOptions<HealthOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var timeout = options.Value.CheckTimeout;

        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        limit.CancelAfter(timeout);

        try
        {
            await redis.Connection.ExecuteAsync("PING").WaitAsync(limit.Token);

            return HealthCheckResult.Healthy("Redis answers.");
        }

        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy($"Redis did not answer within {timeout}.");
        }

        catch (Exception e) when (e is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Redis does not answer.", e);
        }
    }
}
