using Microsoft.Extensions.Logging.Abstractions;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class WorldLoopTest
{
    private static WorldLoop Create() => new(NullLogger<WorldLoop>.Instance);

    [TestMethod]
    public async Task CodeComesBackToTheLoopAfterWaiting()
    {
        var loop = Create();

        var checks = new List<bool>();

        await loop.InvokeAsync(async () =>
        {
            checks.Add(loop.IsCurrent);

            await Task.Delay(10);

            checks.Add(loop.IsCurrent);

            await Task.Run(() => Thread.Sleep(5));

            checks.Add(loop.IsCurrent);
        });

        CollectionAssert.AreEqual(new[] { true, true, true }, checks);
    }

    [TestMethod]
    public async Task WaitingDoesNotHoldOtherWork()
    {
        var loop = Create();

        var slowDone = false;
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var slow = loop.InvokeAsync(async () =>
        {
            await release.Task;

            slowDone = true;
        });

        var finishedFirst = await loop.InvokeAsync(() => !slowDone);

        release.SetResult();

        await slow;

        Assert.IsTrue(finishedFirst);
        Assert.IsTrue(slowDone);
    }

    [TestMethod]
    public async Task WorkRunsOneAtATimeInOrder()
    {
        var loop = Create();

        var running = 0;
        var overlapped = false;
        var order = new List<int>();

        await Task.WhenAll(Enumerable.Range(0, 2000).Select(index => loop.InvokeAsync(() =>
        {
            overlapped |= Interlocked.Increment(ref running) != 1;
            order.Add(index);
            Interlocked.Decrement(ref running);
        })));

        Assert.IsFalse(overlapped);
        CollectionAssert.AreEqual(Enumerable.Range(0, 2000).ToArray(), order);
    }

    [TestMethod]
    public async Task ErrorReachesTheCallerAndTheLoopKeepsRunning()
    {
        var loop = Create();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            loop.InvokeAsync(async () =>
            {
                await Task.Yield();

                throw new InvalidOperationException();
            }));

        Assert.IsTrue(await loop.InvokeAsync(() => loop.IsCurrent));
    }

    [TestMethod]
    public void ServerCodeDoesNotLeaveTheLoop()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);

        while (root is not null && !File.Exists(Path.Join(root.FullName, "OpenWorker.slnx")))
        {
            root = root.Parent;
        }

        Assert.IsNotNull(root, "The solution folder was not found.");

        string[] projects =
        [
            "OpenWorker.AuthServer",
            "OpenWorker.AuthServer.Gameplay",
            "OpenWorker.Channel",
            "OpenWorker.Commands",
            "OpenWorker.DependencyInjection",
            "OpenWorker.DistrictServer",
            "OpenWorker.DistrictServer.Gameplay",
            "OpenWorker.Gameplay",
            "OpenWorker.GateServer",
            "OpenWorker.GateServer.Gameplay",
            "OpenWorker.Hotspot",
            "OpenWorker.Lua",
            "OpenWorker.MazeServer"
        ];

        var found = projects
            .SelectMany(project => Directory.EnumerateFiles(Path.Join(root.FullName, project), "*.cs", SearchOption.AllDirectories))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                           !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(file => File.ReadAllText(file) is var text && (text.Contains(".ConfigureAwait(false)") || text.Contains(".ContinueWith(")))
            .ToArray();

        Assert.IsEmpty(found, string.Join(", ", found));
    }
}
