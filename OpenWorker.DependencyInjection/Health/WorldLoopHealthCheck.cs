using System.Globalization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;

namespace OpenWorker.DependencyInjection.Health;

/// <summary>
/// The world loop answers: an empty callback posted to it runs within <see cref="HealthOptions.LoopTimeout"/>.
/// It is the only check that touches the loop, and the callback does nothing, so it takes no time from the game.
/// A loop that is stuck (a handler that never returns, a callback that blocks) keeps the callback waiting; the next
/// check waits for that same callback instead of queueing another, so a stuck loop collects no pile of probes.
/// </summary>
internal sealed class WorldLoopHealthCheck(WorldLoop loop, IOptions<HealthOptions> options, TimeProvider time) : IHealthCheck
{
    private readonly Lock _lock = new();

    private Task? _probe;
    private long _postedAt;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var timeout = options.Value.LoopTimeout;

        Task probe;
        long postedAt;

        lock (_lock)
        {
            if (_probe is not { IsCompleted: false })
            {
                _probe = loop.InvokeAsync(static () => { });
                _postedAt = time.GetTimestamp();
            }

            probe = _probe;
            postedAt = _postedAt;
        }

        var left = timeout - time.GetElapsedTime(postedAt);

        try
        {
            if (left <= TimeSpan.Zero)
            {
                // The callback of an earlier check is still waiting, and it has waited long enough.
                return Stuck(time.GetElapsedTime(postedAt));
            }

            await probe.WaitAsync(left, time, cancellationToken);

            return HealthCheckResult.Healthy("The world loop answers.");
        }

        catch (TimeoutException)
        {
            return Stuck(time.GetElapsedTime(postedAt));
        }
    }

    private static HealthCheckResult Stuck(TimeSpan waited)
    {
        return HealthCheckResult.Unhealthy(string.Create(CultureInfo.InvariantCulture,
            $"The world loop has not run an empty callback for {waited.TotalSeconds:0.#} s."));
    }
}
