using OpenWorker.Lua;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class MazeTimersTest
{
    private sealed class Host : IMazeTimerHost
    {
        public List<string> Calls { get; } = [];
        public Action? OnComplete { get; set; }

        public ValueTask CallAsync(string name, int first, int second, int third)
        {
            Calls.Add($"{name}({first},{second},{third})");

            return ValueTask.CompletedTask;
        }

        public ValueTask CompleteAsync(int timer)
        {
            Calls.Add($"complete({timer})");

            var action = OnComplete;

            OnComplete = null;
            action?.Invoke();

            return ValueTask.CompletedTask;
        }

        public void ShowTimer(int value, float time, float following)
        {
            Calls.Add($"show({value},{time},{following})");
        }
    }

    private static async Task RunAsync(MazeTimers timers, float seconds, float step = 0.1f)
    {
        for (var time = 0f; time < seconds - step / 2f; time += step)
        {
            await timers.UpdateAsync(step);
        }
    }

    [TestMethod]
    public async Task PlainTimerTellsTheScriptOnceWhenTheTimeIsUp()
    {
        var host = new Host();
        var timers = new MazeTimers(host);

        timers.AddPlain(1051, 1f);

        await RunAsync(timers, 0.8f);
        CollectionAssert.AreEqual(Array.Empty<string>(), host.Calls);

        await RunAsync(timers, 1f);
        CollectionAssert.AreEqual(new[] { "complete(1051)" }, host.Calls);
        Assert.IsTrue(timers.IsEmpty);
    }

    [TestMethod]
    public async Task TimerWithAFunctionHandsOverItsNumbers()
    {
        var host = new Host();
        var timers = new MazeTimers(host);

        timers.AddPlain("OnTimer1", 0.5f, 7, 8, 9);

        await RunAsync(timers, 1f);

        CollectionAssert.AreEqual(new[] { "OnTimer1(7,8,9)" }, host.Calls);
    }

    [TestMethod]
    public async Task ATimerAddedWhileOthersRunStartsWithTheNextRound()
    {
        var host = new Host();
        var timers = new MazeTimers(host);

        host.OnComplete = () => timers.AddPlain(2, 0.05f);

        timers.AddPlain(1, 0.05f);

        await timers.UpdateAsync(0.1f);

        CollectionAssert.AreEqual(new[] { "complete(1)" }, host.Calls);

        await timers.UpdateAsync(0.1f);

        CollectionAssert.AreEqual(new[] { "complete(1)", "complete(2)" }, host.Calls);
    }

    [TestMethod]
    public async Task RemovedTimerSaysNothingAndTellsHowMuchWasLeft()
    {
        var host = new Host();
        var timers = new MazeTimers(host);

        timers.AddPlain("OnTimer2", 5f, 0, 0, 0);

        await RunAsync(timers, 1f);

        var left = timers.Cancel("OnTimer2");

        await RunAsync(timers, 6f);

        Assert.AreEqual(4f, left, 0.3f);
        CollectionAssert.AreEqual(Array.Empty<string>(), host.Calls);
        Assert.IsTrue(timers.IsEmpty);
    }

    [TestMethod]
    public async Task ClearedTimersNeverFire()
    {
        var host = new Host();
        var timers = new MazeTimers(host);

        timers.AddPlain(1, 0.5f);
        timers.AddPlain("OnTimer", 0.5f, 0, 0, 0);

        await timers.UpdateAsync(0.1f);

        timers.Clear();

        await RunAsync(timers, 2f);

        CollectionAssert.AreEqual(Array.Empty<string>(), host.Calls);
    }

    [TestMethod]
    public async Task SteppedTimerWaitsThenCountsEachStep()
    {
        var host = new Host();
        var timers = new MazeTimers(host);

        // Two steps of three seconds, with a second of waiting before each.
        timers.AddStepped("OnFinish", "OnStep", "OnReady", 3f, 2, 1);

        await RunAsync(timers, 0.5f);
        CollectionAssert.AreEqual(new[] { "OnReady(1,0,0)" }, host.Calls);

        await RunAsync(timers, 1f);
        Assert.AreEqual("show(0,3,0)", host.Calls[1], "the waiting is over: the step is shown");

        await RunAsync(timers, 3f);
        CollectionAssert.AreEqual(new[] { "OnReady(1,0,0)", "show(0,3,0)", "OnFinish(1,0,0)", "show(-1,-1,0)" }, host.Calls);

        // The script starts it again for the next step.
        timers.Start("OnFinish");
        host.Calls.Clear();

        await RunAsync(timers, 0.5f);
        CollectionAssert.AreEqual(new[] { "OnReady(2,0,0)" }, host.Calls);

        await RunAsync(timers, 4.5f);
        CollectionAssert.AreEqual(new[] { "OnReady(2,0,0)", "show(0,3,0)", "OnFinish(2,0,0)", "show(-1,-1,0)" }, host.Calls);
        Assert.IsTrue(timers.IsEmpty, "the last step ends the timer");
    }

    [TestMethod]
    public async Task ScriptThatFinishesAStepEarlyGivesItsTimeToTheNextOne()
    {
        var host = new Host();
        var timers = new MazeTimers(host);

        timers.AddStepped("OnFinish", "OnStep", "OnReady", 10f, 2, 1);

        await RunAsync(timers, 4f);

        timers.Complete("OnFinish", 1);

        await RunAsync(timers, 0.3f);

        Assert.IsTrue(host.Calls.Contains("OnStep(1,0,0)"), "the early finish goes to its own function");
        Assert.IsFalse(host.Calls.Any(e => e.StartsWith("OnFinish", StringComparison.Ordinal)));

        timers.Start("OnFinish");
        host.Calls.Clear();

        await RunAsync(timers, 1.5f);

        // About seven seconds were left of the first step, and the next one has them on top of its own ten.
        var shown = host.Calls.First(e => e.StartsWith("show(0,", StringComparison.Ordinal));

        StringAssert.StartsWith(shown, "show(0,16,");
    }

    [TestMethod]
    public async Task ExtendedTimerShowsWhatIsLeft()
    {
        var host = new Host();
        var timers = new MazeTimers(host);

        timers.AddStepped("OnFinish", "OnStep", "OnReady", 10f, 1, 0);

        await RunAsync(timers, 2f);

        host.Calls.Clear();
        timers.Extend("OnFinish", 60f);

        StringAssert.StartsWith(host.Calls.Single(), "show(0,");
    }
}
