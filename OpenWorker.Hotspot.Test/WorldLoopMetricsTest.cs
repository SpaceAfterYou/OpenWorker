using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Hotspot.Telemetry;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class WorldLoopMetricsTest
{
    [TestMethod]
    public async Task EveryCallbackRecordsHowLongItHeldTheLoop()
    {
        using var metrics = new MetricsHost();
        using var durations = metrics.Listen<double>("worldloop.callback.duration");
        using var loop = new WorldLoop(NullLogger<WorldLoop>.Instance, metrics.Get<WorldLoopMetrics>());

        await loop.InvokeAsync(() => Thread.Sleep(40));

        // The callback completes its task before the loop records it.
        await MetricAssertions.UntilAsync(
            () => durations.GetMeasurementSnapshot().Any(e => e.Value >= 0.035),
            "The callback that held the loop for 40 ms was not recorded.");

        Assert.IsTrue(durations.GetMeasurementSnapshot().All(e => e.Value is >= 0 and < 5), "A duration is in seconds.");
    }

    [TestMethod]
    public async Task ACallbackThatFailsIsRecordedToo()
    {
        using var metrics = new MetricsHost();
        using var durations = metrics.Listen<double>("worldloop.callback.duration");
        using var loop = new WorldLoop(NullLogger<WorldLoop>.Instance, metrics.Get<WorldLoopMetrics>());

        loop.Post(_ =>
        {
            Thread.Sleep(30);

            throw new InvalidOperationException("A callback that fails.");
        }, null);

        await MetricAssertions.UntilAsync(
            () => durations.GetMeasurementSnapshot().Any(e => e.Value >= 0.025),
            "The callback that failed was not recorded.");
    }

    [TestMethod]
    public async Task QueueLengthIsTheCallbacksThatWaitForTheThread()
    {
        using var metrics = new MetricsHost();
        using var queue = metrics.Listen<int>("worldloop.queue.length");
        using var loop = new WorldLoop(NullLogger<WorldLoop>.Instance, metrics.Get<WorldLoopMetrics>());

        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();

        var holding = loop.InvokeAsync(() =>
        {
            started.SetResult();

            release.Wait();
        });

        // The one that holds the thread is no longer in the queue.
        await started.Task;

        var waiting = Enumerable.Range(0, 5).Select(_ => loop.InvokeAsync(() => { })).ToArray();

        queue.RecordObservableInstruments();

        Assert.AreEqual(5, queue.LastMeasurement!.Value);

        release.Set();

        await Task.WhenAll(waiting.Append(holding));

        queue.RecordObservableInstruments();

        Assert.AreEqual(0, queue.LastMeasurement.Value);
    }

    [TestMethod]
    public void AStoppedLoopReportsAnEmptyQueue()
    {
        using var metrics = new MetricsHost();
        using var queue = metrics.Listen<int>("worldloop.queue.length");
        var loop = new WorldLoop(NullLogger<WorldLoop>.Instance, metrics.Get<WorldLoopMetrics>());

        loop.Dispose();

        queue.RecordObservableInstruments();

        Assert.AreEqual(0, queue.LastMeasurement!.Value);
    }
}
