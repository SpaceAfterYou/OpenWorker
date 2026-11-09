using System.Diagnostics;
using System.Numerics;

namespace OpenWorker.LoadTest.Stats;

/// <summary>
/// Counts durations in microseconds in buckets that grow with the value: exact below 32 µs, and 32 buckets for
/// every power of two above, so a percentile is off by 3% at most. A percentile is reported as the top of its
/// bucket, never below the real value; the largest value is kept exactly.
/// <para>
/// One thread records into a histogram at a time (a bot records from its own read loop), so the counters are
/// plain. Another thread may read it while it is written: it sees a count or two behind, never a torn one, which
/// is all a progress line or the last report needs. Many histograms are added together with <see cref="Add" />.
/// </para>
/// </summary>
internal sealed class LatencyHistogram
{
    private const int SubBits = 5;
    private const int SubCount = 1 << SubBits;

    /// <summary>
    /// The longest value that gets a bucket of its own, 2^36 µs, about 19 hours; a longer one is counted in the last bucket.
    /// </summary>
    private const int TopBit = 36;

    private const int BucketCount = (TopBit - SubBits + 2) << SubBits;

    private static readonly double MicrosecondsPerTick = 1_000_000d / Stopwatch.Frequency;

    // Made by the first value, so a histogram that is never used costs nothing: most bots use only some of theirs.
    private uint[]? _counts;

    private long _total;
    private long _sum;
    private long _max;

    public long Count => Volatile.Read(ref _total);

    public long MaxMicroseconds => Volatile.Read(ref _max);

    public double MeanMicroseconds => _total == 0 ? 0 : (double)Volatile.Read(ref _sum) / _total;

    /// <summary>
    /// The time between two readings of <see cref="Stopwatch.GetTimestamp" />, in microseconds.
    /// </summary>
    public static long ToMicroseconds(long startTimestamp, long endTimestamp)
    {
        return (long)((endTimestamp - startTimestamp) * MicrosecondsPerTick);
    }

    public void Record(long microseconds)
    {
        if (microseconds < 0)
        {
            microseconds = 0;
        }

        var counts = _counts;

        if (counts is null)
        {
            counts = new uint[BucketCount];

            Volatile.Write(ref _counts, counts);
        }

        counts[BucketOf(microseconds)]++;
        _total++;
        _sum += microseconds;

        if (microseconds > _max)
        {
            _max = microseconds;
        }
    }

    public void RecordTicks(long startTimestamp, long endTimestamp)
    {
        Record(ToMicroseconds(startTimestamp, endTimestamp));
    }

    /// <summary>
    /// Adds what another histogram has counted.
    /// </summary>
    public void Add(LatencyHistogram other)
    {
        if (other._counts is not { } theirs)
        {
            return;
        }

        var ours = _counts ??= new uint[BucketCount];

        for (var i = 0; i < ours.Length; i++)
        {
            ours[i] += theirs[i];
        }

        _total += other._total;
        _sum += other._sum;
        _max = Math.Max(_max, other._max);
    }

    /// <summary>
    /// The value that the given share of the recorded values does not exceed (0.99 is the 99th percentile), in
    /// microseconds; zero when nothing was recorded.
    /// </summary>
    public long Percentile(double share)
    {
        var counts = Volatile.Read(ref _counts);

        if (_total == 0 || counts is null)
        {
            return 0;
        }

        var rank = (long)Math.Ceiling(Math.Clamp(share, 0d, 1d) * _total);

        rank = Math.Max(rank, 1);

        long seen = 0;

        for (var i = 0; i < counts.Length; i++)
        {
            seen += counts[i];

            if (seen >= rank)
            {
                return Math.Min(UpperBoundOf(i), _max);
            }
        }

        return _max;
    }

    private static int BucketOf(long value)
    {
        if (value < SubCount)
        {
            return (int)value;
        }

        var bit = BitOperations.Log2((ulong)value);

        if (bit > TopBit)
        {
            return BucketCount - 1;
        }

        var shift = bit - SubBits;

        return ((shift + 1) << SubBits) + (int)((value >> shift) - SubCount);
    }

    private static long UpperBoundOf(int bucket)
    {
        if (bucket < SubCount)
        {
            return bucket;
        }

        var shift = (bucket >> SubBits) - 1;
        var low = (long)(SubCount + (bucket & (SubCount - 1))) << shift;

        return low + (1L << shift) - 1;
    }
}
