using System.Collections.Concurrent;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class RegistrationServiceTest
{
    /// <summary>
    /// A service with a loop that needs a moment to end, like a renewal that is in flight when the server stops.
    /// </summary>
    private sealed class Probe(bool failsToRemove = false) : RegistrationService
    {
        public ConcurrentQueue<string> Events { get; } = new();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Events.Enqueue("loop started");

            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }

            catch (OperationCanceledException)
            {
                await Task.Delay(50, CancellationToken.None);

                Events.Enqueue("loop ended");
            }
        }

        protected override Task RemoveAsync(CancellationToken cancellationToken)
        {
            Events.Enqueue("removed");

            return failsToRemove ? Task.FromException(new InvalidOperationException("Redis is down.")) : Task.CompletedTask;
        }
    }

    private static async Task<Probe> StartAsync(Probe probe, CancellationToken cancellationToken)
    {
        await probe.StartAsync(cancellationToken);

        // The loop runs on its own thread from the start; wait until it does.
        while (!probe.Events.Contains("loop started"))
        {
            await Task.Delay(5, cancellationToken);
        }

        return probe;
    }

    [TestMethod]
    public async Task RecordIsRemovedOnlyAfterTheLoopHasEnded()
    {
        var probe = await StartAsync(new Probe(), TestContext.CancellationToken);

        await probe.WithdrawAsync(TestContext.CancellationToken);

        // A renewal that is still running when the record is removed would write it again.
        Assert.AreEqual("loop started, loop ended, removed", string.Join(", ", probe.Events));
    }

    [TestMethod]
    public async Task WithdrawingAndThenStoppingRemovesTheRecordOnce()
    {
        var probe = await StartAsync(new Probe(), TestContext.CancellationToken);

        await probe.WithdrawAsync(TestContext.CancellationToken);
        await probe.WithdrawAsync(TestContext.CancellationToken);

        // The host stops the service after the drain has withdrawn it.
        await probe.StopAsync(TestContext.CancellationToken);

        Assert.AreEqual(1, probe.Events.Count(e => e == "removed"));
    }

    [TestMethod]
    public async Task SeveralCallsAtOnceRemoveTheRecordOnce()
    {
        var probe = await StartAsync(new Probe(), TestContext.CancellationToken);

        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => probe.WithdrawAsync(TestContext.CancellationToken), TestContext.CancellationToken)));

        Assert.AreEqual(1, probe.Events.Count(e => e == "removed"));
    }

    [TestMethod]
    public async Task StoppingTheHostWithoutADrainWithdrawsTheService()
    {
        var probe = await StartAsync(new Probe(), TestContext.CancellationToken);

        await probe.StopAsync(TestContext.CancellationToken);

        Assert.AreEqual("loop started, loop ended, removed", string.Join(", ", probe.Events));
    }

    [TestMethod]
    public async Task ServiceThatWasNeverStartedStillTakesItsRecordBack()
    {
        var probe = new Probe();

        await probe.WithdrawAsync(TestContext.CancellationToken);

        Assert.AreEqual("removed", string.Join(", ", probe.Events));
    }

    [TestMethod]
    public async Task FailureToRemoveReachesTheCallerAndEveryLaterCall()
    {
        var probe = await StartAsync(new Probe(failsToRemove: true), TestContext.CancellationToken);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => probe.WithdrawAsync(TestContext.CancellationToken));

        // Once, not once more for the stop of the host.
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => probe.StopAsync(TestContext.CancellationToken));

        Assert.AreEqual(1, probe.Events.Count(e => e == "removed"));
    }

    public TestContext TestContext { get; set; } = null!;
}
