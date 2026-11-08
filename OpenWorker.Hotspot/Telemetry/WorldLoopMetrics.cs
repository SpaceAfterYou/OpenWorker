using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace OpenWorker.Hotspot.Telemetry;

/// <summary>
/// What the world loop reports: how many callbacks wait for the thread, and how long the thread is held by each.<br/>
/// A callback is anything posted to the loop: the start of a handler, the continuation after each of its awaits, a timer,
/// the closing of a connection. A long one holds up every other connection, so the histogram of these is the lag of the server.
/// </summary>
public sealed class WorldLoopMetrics
{
    /// <summary>
    /// What <see cref="Start"/> returns when nobody listens: no timestamp was taken and nothing is recorded.
    /// </summary>
    private const long NotMeasured = 0;

    private readonly Meter _meter;
    private readonly Histogram<double> _callbackDuration;

    public WorldLoopMetrics(IMeterFactory factory)
    {
        _meter = OpenWorkerMeter.Create(factory);

        _callbackDuration = _meter.CreateDuration(
            "worldloop.callback.duration",
            "How long one callback holds the thread of the world loop.");
    }

    /// <summary>
    /// Reports the callbacks that wait in the queue of a loop. Called once, by the loop itself; the process has one loop.
    /// </summary>
    internal void ObserveQueue(Func<int> length)
    {
        _meter.CreateObservableGauge(
            "worldloop.queue.length",
            length,
            unit: "{callback}",
            description: "Callbacks posted to the world loop that have not started yet.");
    }

    /// <summary>
    /// The moment a callback starts, or nothing when no one listens, so that an unobserved server does not read the clock.
    /// </summary>
    internal long Start()
    {
        return _callbackDuration.Enabled ? Stopwatch.GetTimestamp() : NotMeasured;
    }

    internal void Finish(long started)
    {
        if (started != NotMeasured)
        {
            _callbackDuration.Record(Stopwatch.GetElapsedTime(started).TotalSeconds);
        }
    }
}
