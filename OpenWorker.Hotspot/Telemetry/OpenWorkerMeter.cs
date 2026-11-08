using System.Diagnostics.Metrics;

namespace OpenWorker.Hotspot.Telemetry;

/// <summary>
/// The one <see cref="Meter"/> of the server: every instrument of every area (world loop, handlers, sessions, Redis,
/// Postgres) is created on it, so a listener or an exporter asks for <see cref="Name"/> and gets them all.<br/>
/// The meter comes from <see cref="IMeterFactory"/>, not from a static field: the factory gives the same meter to every
/// class that asks for the name inside one container, and a test that builds its own container sees only its own
/// measurements.
/// </summary>
public static class OpenWorkerMeter
{
    public const string Name = "OpenWorker";

    /// <summary>
    /// Where the bucket edges of the duration histograms fall, in seconds. The default edges of an exporter are made for
    /// milliseconds, and every duration of this server would land in the first bucket of them.<br/>
    /// The edges stop at 50 ms and 100 ms on purpose: 50 ms is the step of <c>SimulationClock</c>, 100 ms is the limit the
    /// plan sets for a movement packet.
    /// </summary>
    private static readonly double[] LatencyEdges =
    [
        0.0001, 0.00025, 0.0005, 0.001, 0.0025, 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 5
    ];

    internal static Meter Create(IMeterFactory factory)
    {
        return factory.Create(new MeterOptions(Name));
    }

    /// <summary>
    /// A histogram of durations in seconds with the bucket edges of <see cref="LatencyEdges"/>.
    /// </summary>
    internal static Histogram<double> CreateDuration(this Meter meter, string name, string description)
    {
        return meter.CreateHistogram(
            name,
            unit: "s",
            description,
            tags: null,
            new InstrumentAdvice<double> { HistogramBucketBoundaries = LatencyEdges });
    }
}
