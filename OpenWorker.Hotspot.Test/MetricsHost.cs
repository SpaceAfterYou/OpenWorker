using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using OpenWorker.Hotspot.Telemetry;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A container with the metric classes of the server and nothing else. Its meters belong to its own
/// <see cref="IMeterFactory"/>, so a collector made here sees only what the test under it records, however many tests run at once.
/// </summary>
internal sealed class MetricsHost : IDisposable
{
    private readonly ServiceProvider _services = new ServiceCollection().AddOpenWorkerMetrics().AddRedisMetrics().AddPostgresMetrics().BuildServiceProvider();

    public IMeterFactory Factory => _services.GetRequiredService<IMeterFactory>();

    public T Get<T>() where T : notnull => _services.GetRequiredService<T>();

    /// <summary>
    /// Listens to one instrument of the <c>OpenWorker</c> meter.
    /// </summary>
    public MetricCollector<T> Listen<T>(string instrument) where T : struct => new(Factory, OpenWorkerMeter.Name, instrument);

    public void Dispose()
    {
        _services.Dispose();
    }
}

internal static class MetricAssertions
{
    /// <summary>
    /// Everything recorded so far, summed: the value of a counter.
    /// </summary>
    public static long Sum(this MetricCollector<long> collector) => collector.GetMeasurementSnapshot().Sum(e => e.Value);

    /// <summary>
    /// Everything recorded so far under one value of a tag, summed.
    /// </summary>
    public static long Sum(this MetricCollector<long> collector, string tag, string value)
    {
        return collector.GetMeasurementSnapshot()
            .Where(e => e.Tags.TryGetValue(tag, out var found) && Equals(found, value))
            .Sum(e => e.Value);
    }

    /// <summary>
    /// The values recorded under one value of a tag.
    /// </summary>
    public static double[] Values(this MetricCollector<double> collector, string tag, string value)
    {
        return collector.GetMeasurementSnapshot()
            .Where(e => e.Tags.TryGetValue(tag, out var found) && Equals(found, value))
            .Select(e => e.Value)
            .ToArray();
    }

    /// <summary>
    /// Waits for what the code under test does on a task of its own, such as the end of the write loop of a connection.
    /// </summary>
    public static async Task UntilAsync(Func<bool> condition, string because, long timeoutMilliseconds = 10_000)
    {
        var timer = Stopwatch.StartNew();

        while (!condition())
        {
            Assert.IsLessThan(timeoutMilliseconds, timer.ElapsedMilliseconds, because);

            await Task.Delay(10);
        }
    }
}
