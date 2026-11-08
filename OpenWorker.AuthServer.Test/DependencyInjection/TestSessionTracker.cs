using OpenWorker.DependencyInjection;

namespace OpenWorker.AuthServer.Test.DependencyInjection;

[TestClass]
public sealed class TestSessionTracker
{
    [TestMethod]
    public async Task NothingToWaitForWhenNoConnectionIsServed()
    {
        var tracker = new SessionTracker();

        await tracker.WhenIdle().WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        Assert.AreEqual(0, tracker.Count);
    }

    [TestMethod]
    public async Task WaitsForTheLastConnectionToEnd()
    {
        var tracker = new SessionTracker();

        tracker.Enter();
        tracker.Enter();

        var idle = tracker.WhenIdle();

        tracker.Exit();

        await Task.Delay(50, TestContext.CancellationToken);

        Assert.IsFalse(idle.IsCompleted, "One connection is still served.");
        Assert.AreEqual(1, tracker.Count);

        tracker.Exit();

        await idle.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task IdleAgainAfterNewConnectionsCameAndWent()
    {
        var tracker = new SessionTracker();

        tracker.Enter();
        tracker.Exit();

        await tracker.WhenIdle().WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        // The completed wait of before must not stand in for this one.
        tracker.Enter();

        var idle = tracker.WhenIdle();

        Assert.IsFalse(idle.IsCompleted);

        tracker.Exit();

        await idle.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
    }

    public TestContext TestContext { get; set; } = null!;
}
