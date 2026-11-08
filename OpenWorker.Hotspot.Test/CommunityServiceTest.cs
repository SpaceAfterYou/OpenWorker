using Arch.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Channel.Community;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The service that keeps the people of a server on the list: one round that fails must not end it.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class CommunityServiceTest
{
    private sealed class RecordingLogger<T> : ILogger<T>
    {
        private int _errors;

        public int Errors => Volatile.Read(ref _errors);

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Error)
            {
                Interlocked.Increment(ref _errors);
            }
        }
    }

    /// <summary>
    /// Describes the people the way the server does, until it is told to fail.
    /// </summary>
    private sealed class FlakySource(IPresenceSource inner) : IPresenceSource
    {
        private int _described;

        public volatile bool Fails;

        public int Described => Volatile.Read(ref _described);

        public PresenceRecord Describe(Entity player, Guid instance)
        {
            if (Fails)
            {
                throw new InvalidOperationException("The person has lost a component.");
            }

            Interlocked.Increment(ref _described);

            return inner.Describe(player, instance);
        }
    }

    /// <summary>
    /// Lets time pass until the condition holds. The service makes its timer a moment after it starts,
    /// and time that passed before that fires nothing, so one jump is not enough.
    /// </summary>
    private static async Task AdvanceUntilAsync(FakeTimeProvider time, Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);

        while (!condition())
        {
            Assert.IsLessThan(deadline, DateTime.UtcNow, "The condition was not met in time.");

            time.Advance(CommunityNetwork.PresenceRenewal);

            await Task.Delay(20);
        }
    }

    [TestMethod]
    public async Task ARoundThatFailsIsLoggedAndTheNextOneStillRuns()
    {
        FlakySource? source = null;

        using var server = new TestServer(new MemoryCommunity(), wrapSource: inner => source = new FlakySource(inner));
        using var player = server.Join(1, "Anna");

        var time = new FakeTimeProvider();
        var logger = new RecordingLogger<CommunityService>();
        var service = new CommunityService(server.Network, server.Loop, [], time, logger);

        Assert.IsNotNull(source);

        source.Fails = true;

        await service.StartAsync(CancellationToken.None);

        await AdvanceUntilAsync(time, () => logger.Errors >= 1);

        Assert.IsFalse(service.ExecuteTask!.IsCompleted, "One failed round must not stop the service.");

        // The person is looked at again once the fault is gone.
        var described = source.Described;

        source.Fails = false;

        await AdvanceUntilAsync(time, () => source.Described > described);

        await service.StopAsync(CancellationToken.None);

        Assert.IsTrue(service.ExecuteTask.IsCompletedSuccessfully, "Stopping must end the loop without an error.");
    }

    [TestMethod]
    public async Task StoppingTheServiceEndsTheLoopWithoutAnError()
    {
        using var server = new TestServer(new MemoryCommunity());
        using var player = server.Join(1, "Anna");

        var time = new FakeTimeProvider();
        var logger = new RecordingLogger<CommunityService>();
        var service = new CommunityService(server.Network, server.Loop, [], time, logger);

        await service.StartAsync(CancellationToken.None);

        for (var period = 0; period < 3; period++)
        {
            time.Advance(CommunityNetwork.PresenceRenewal);

            await server.Loop.InvokeAsync(() => { });
        }

        await service.StopAsync(CancellationToken.None);

        Assert.IsTrue(service.ExecuteTask!.IsCompletedSuccessfully);
        Assert.AreEqual(0, logger.Errors);
    }
}
