namespace OpenWorker.LoadTest.Stats;

/// <summary>
/// The figures of a histogram that the report shows, in milliseconds.
/// </summary>
internal sealed record LatencySummary(long Count, double MeanMs, double P50Ms, double P95Ms, double P99Ms, double MaxMs)
{
    public static LatencySummary Empty { get; } = new(0, 0, 0, 0, 0, 0);

    public static LatencySummary Of(LatencyHistogram histogram)
    {
        var count = histogram.Count;

        if (count == 0)
        {
            return Empty;
        }

        return new LatencySummary(
            count,
            histogram.MeanMicroseconds / 1000d,
            histogram.Percentile(0.50) / 1000d,
            histogram.Percentile(0.95) / 1000d,
            histogram.Percentile(0.99) / 1000d,
            histogram.MaxMicroseconds / 1000d);
    }
}
