using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace OpenWorker.Hotspot.Telemetry;

/// <summary>
/// How long each handler takes, why packets fail and which are dropped for their rate.<br/>
/// The tag of the duration is the name of the opcode, worked out once for each slot of the handler table, so
/// measuring a packet allocates nothing.
/// </summary>
public sealed class HandlerMetrics
{
    private const long NotMeasured = 0;

    private readonly Histogram<double> _duration;
    private readonly Counter<long> _errors;
    private readonly Counter<long> _refused;

    public HandlerMetrics(IMeterFactory factory)
    {
        var meter = OpenWorkerMeter.Create(factory);

        _duration = meter.CreateDuration(
            "handler.duration",
            "How long a handler runs, from the start to the end of its task, so the waits for Redis and the database are in it. Tag opcode.");

        _errors = meter.CreateCounter<long>(
            "handler.errors",
            unit: "{error}",
            description: "Packets that failed to parse or to handle. Tag reason.");

        _refused = meter.CreateCounter<long>(
            "handler.refused",
            unit: "{packet}",
            description: "Packets dropped because the session sent the packets of their group faster than its limit. Tag group.");
    }

    internal long Start()
    {
        return _duration.Enabled ? Stopwatch.GetTimestamp() : NotMeasured;
    }

    /// <param name="opcode">The name of the opcode, the tag of the measurement.</param>
    /// <param name="started">What <see cref="Start"/> returned.</param>
    internal void Finish(string opcode, long started)
    {
        if (started != NotMeasured)
        {
            _duration.Record(Stopwatch.GetElapsedTime(started).TotalSeconds, new KeyValuePair<string, object?>("opcode", opcode));
        }
    }

    /// <param name="group">The tag of the group, from <c>PacketRateLimiter.Tag</c>.</param>
    internal void Refused(string group)
    {
        _refused.Add(1, new KeyValuePair<string, object?>("group", group));
    }

    /// <param name="reason">The tag of the reason, from <c>HandlerErrorBudget.Tag</c>.</param>
    internal void Error(string reason)
    {
        _errors.Add(1, new KeyValuePair<string, object?>("reason", reason));
    }
}
