using System.Diagnostics.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using OpenWorker.DependencyInjection;
using OpenWorker.Hotspot.Telemetry;
using Redis.OM.Contracts;
using StackExchange.Redis;

namespace OpenWorker.AuthServer.Test;

/// <summary>
/// The Redis calls of the process are timed on its one connection. Needs Redis with the search module:
/// set OPENWORKER_TEST_REDIS, for example redis://127.0.0.1:6379.
/// </summary>
[TestClass]
public sealed class TestRedisMetrics
{
    [TestMethod]
    public async Task RedisCallsOfTheCacheAndOfTheCodeAroundItAreTimedByTheNameOfTheCommand()
    {
        var address = Environment.GetEnvironmentVariable("OPENWORKER_TEST_REDIS");

        if (string.IsNullOrEmpty(address))
        {
            Assert.Inconclusive("OPENWORKER_TEST_REDIS is not set.");
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Redis"] = address })
            .Build();

        await using var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddCache()
            .BuildServiceProvider();

        var redis = services.GetRequiredService<IConnectionMultiplexer>();
        var cache = services.GetRequiredService<IRedisConnectionProvider>();
        var metrics = services.GetRequiredService<RedisMetrics>();

        var key = $"ow:test:telemetry:{Guid.NewGuid():N}";

        // Nobody listens yet: the profiler collects nothing, so an unobserved server pays for nothing.
        await redis.GetDatabase().StringSetAsync(key, "1");

        Assert.AreEqual(0, metrics.Flush());

        using var durations = new MetricCollector<double>(services.GetRequiredService<IMeterFactory>(), OpenWorkerMeter.Name, "redis.call.duration");

        // One call straight to StackExchange.Redis, as the presence store makes them, and two through Redis.OM:
        // they share the connection, so one profiler sees all three.
        await redis.GetDatabase().StringGetAsync(key);
        await cache.Connection.ExecuteAsync("GET", key);
        await cache.Connection.ExecuteAsync("DEL", key);

        Assert.IsGreaterThanOrEqualTo(3, metrics.Flush());

        var measurements = durations.GetMeasurementSnapshot();
        var operations = measurements.Select(e => (string)e.Tags["operation"]!).ToArray();

        Assert.AreEqual(1, operations.Count(e => e == "DEL"));
        Assert.AreEqual(2, operations.Count(e => e == "GET"), "The call of the presence store and the call of the cache.");
        Assert.IsTrue(measurements.All(e => e.Value is > 0 and < 5), "A duration is in seconds.");
    }
}
