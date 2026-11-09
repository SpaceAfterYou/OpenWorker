using System.Diagnostics.Metrics;
using StackExchange.Redis;
using StackExchange.Redis.Profiling;

namespace OpenWorker.Hotspot.Telemetry;

/// <summary>
/// How long the calls to Redis take, tagged with the name of the Redis command (<c>GET</c>, <c>JSON.SET</c>,
/// <c>FT.SEARCH</c>, ...).<br/>
/// It listens on the connection itself instead of wrapping the code that calls Redis: the cache (Redis.OM), the presence
/// store, the notifications and the scripts all use one <see cref="IConnectionMultiplexer"/>, and its profiler sees each
/// command with the time the multiplexer measured, from the moment it was made to the moment its answer came back.
/// The calls of the world loop are not slowed down or moved to another thread, and nothing wraps their tasks.<br/>
/// The profiler collects commands into a session; <see cref="Flush"/> turns them into measurements, once a second
/// (<c>RedisMetricsService</c>). While no one listens to the histogram the profiler returns no session and collects nothing.
/// </summary>
public sealed class RedisMetrics
{
    private readonly Histogram<double> _duration;
    private readonly ProfilingSession _session = new();

    public RedisMetrics(IMeterFactory factory)
    {
        var meter = OpenWorkerMeter.Create(factory);

        _duration = meter.CreateDuration(
            "redis.call.duration",
            "How long a Redis command takes, from the moment it is made to its answer. Tag operation: the name of the command.");
    }

    /// <summary>
    /// Measures every command that <paramref name="multiplexer"/> sends from now on.
    /// </summary>
    public void Attach(IConnectionMultiplexer multiplexer)
    {
        multiplexer.RegisterProfiler(GetSession);
    }

    private ProfilingSession? GetSession()
    {
        return _duration.Enabled ? _session : null;
    }

    /// <summary>
    /// Records the commands that finished since the last call, and returns how many they were. Safe from any thread.
    /// </summary>
    public int Flush()
    {
        var count = 0;

        foreach (var command in _session.FinishProfiling())
        {
            _duration.Record(command.ElapsedTime.TotalSeconds, new KeyValuePair<string, object?>("operation", command.Command));

            count++;
        }

        return count;
    }
}
