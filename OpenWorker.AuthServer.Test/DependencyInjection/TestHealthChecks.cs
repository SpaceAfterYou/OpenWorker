using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.DependencyInjection;
using OpenWorker.DependencyInjection.Health;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Persistence;
using Redis.OM;
using Redis.OM.Contracts;

namespace OpenWorker.AuthServer.Test.DependencyInjection;

/// <summary>
/// Each check on its own: what makes it healthy, what makes it fail, and that it does not wait longer than its
/// timeout. The endpoint that serves them is tested in <see cref="TestHealthEndpoint"/>.
/// </summary>
[TestClass]
public sealed class TestHealthChecks
{
    private static readonly TimeSpan Second = TimeSpan.FromSeconds(1);

    private static IOptions<HealthOptions> Settings(TimeSpan? loop = null, TimeSpan? check = null)
    {
        return Microsoft.Extensions.Options.Options.Create(new HealthOptions
        {
            Port = 8080,
            LoopTimeout = loop ?? TimeSpan.FromSeconds(5),
            CheckTimeout = check ?? TimeSpan.FromSeconds(5)
        });
    }

    private static WorldLoop CreateLoop() => new(NullLogger<WorldLoop>.Instance);

    private static HealthCheckContext Context() => new();

    private sealed class TestLifetime : IHostApplicationLifetime, IDisposable
    {
        private readonly CancellationTokenSource _stopping = new();

        public CancellationToken ApplicationStarted => CancellationToken.None;

        public CancellationToken ApplicationStopping => _stopping.Token;

        public CancellationToken ApplicationStopped => CancellationToken.None;

        public void StopApplication() => _stopping.Cancel();

        public void Dispose() => _stopping.Dispose();
    }

    [TestMethod]
    public async Task LoopThatAnswersIsHealthyAndTheCheckDoesNotRunOnIt()
    {
        using var loop = CreateLoop();

        var check = new WorldLoopHealthCheck(loop, Settings(), TimeProvider.System);

        var result = await check.CheckHealthAsync(Context(), TestContext.CancellationToken);

        Assert.AreEqual(HealthStatus.Healthy, result.Status);
        Assert.IsFalse(loop.IsCurrent, "The check runs where it is called; only the empty callback runs on the loop.");
    }

    [TestMethod]
    public async Task StuckLoopIsUnhealthyAndDoesNotCollectProbes()
    {
        using var loop = CreateLoop();

        var time = new FakeTimeProvider();
        var check = new WorldLoopHealthCheck(loop, Settings(loop: TimeSpan.FromSeconds(5)), time);

        using var release = new ManualResetEventSlim();

        // A callback that never returns, which is what a handler stuck in a lock or a loop looks like to the others.
        var stuck = loop.InvokeAsync(() => release.Wait(TestContext.CancellationToken));

        var first = check.CheckHealthAsync(Context(), TestContext.CancellationToken);

        // The check waits for its timeout, which is time that passes only when the test says so.
        while (!first.IsCompleted)
        {
            time.Advance(TimeSpan.FromSeconds(5));

            await Task.Delay(10, TestContext.CancellationToken);
        }

        var unhealthy = await first;

        Assert.AreEqual(HealthStatus.Unhealthy, unhealthy.Status);
        StringAssert.Contains(unhealthy.Description, "world loop");

        // The empty callback of the first check has been waiting longer than the timeout, so the second check has
        // nothing to wait for. If it queued a callback of its own it would wait for it here, with no time passing.
        var second = await check.CheckHealthAsync(Context(), TestContext.CancellationToken).WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);

        Assert.AreEqual(HealthStatus.Unhealthy, second.Status);

        release.Set();

        await stuck;

        // The empty probe of the first check was queued behind the stuck callback and runs right after it, on the
        // loop thread; this test resumes on another thread, so it waits for the loop to get there.
        await loop.InvokeAsync(static () => { });

        var recovered = await check.CheckHealthAsync(Context(), TestContext.CancellationToken);

        Assert.AreEqual(HealthStatus.Healthy, recovered.Status);
    }

    [TestMethod]
    public async Task ServerIsReadyOnlyWhileItListensAndDoesNotDrain()
    {
        using var lifetime = new TestLifetime();

        var state = new ServerState(lifetime);
        var check = new ServerStateHealthCheck(state);

        var starting = await check.CheckHealthAsync(Context(), TestContext.CancellationToken);

        Assert.AreEqual(HealthStatus.Unhealthy, starting.Status);
        StringAssert.Contains(starting.Description, "not started");

        state.MarkListening(10000);

        Assert.AreEqual(HealthStatus.Healthy, (await check.CheckHealthAsync(Context(), TestContext.CancellationToken)).Status);

        // The signal to stop (SIGTERM) is enough: no service has to stop for readiness to turn off.
        lifetime.StopApplication();

        var draining = await check.CheckHealthAsync(Context(), TestContext.CancellationToken);

        Assert.IsTrue(state.IsListening);
        Assert.IsTrue(state.IsDraining);
        Assert.AreEqual(HealthStatus.Unhealthy, draining.Status);
        StringAssert.Contains(draining.Description, "stopping");
    }

    private sealed class DatabaseStub(Func<CancellationToken, Task<PersistenceContext>> create) : IDbContextFactory<PersistenceContext>
    {
        public PersistenceContext CreateDbContext() => throw new NotSupportedException();

        public Task<PersistenceContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => create(cancellationToken);
    }

    [TestMethod]
    public async Task DatabaseThatAcceptsConnectionsIsHealthy()
    {
        await using var provider = new ServiceCollection().AddLogging().AddTestPersistence().BuildServiceProvider();

        var check = new PostgresHealthCheck(provider.GetRequiredService<IDbContextFactory<PersistenceContext>>(), Settings());

        var result = await check.CheckHealthAsync(Context(), TestContext.CancellationToken);

        Assert.AreEqual(HealthStatus.Healthy, result.Status);
    }

    [TestMethod]
    public async Task DatabaseThatFailsIsUnhealthyWithTheException()
    {
        var error = new InvalidOperationException("no connection");
        var check = new PostgresHealthCheck(new DatabaseStub(_ => Task.FromException<PersistenceContext>(error)), Settings());

        var result = await check.CheckHealthAsync(Context(), TestContext.CancellationToken);

        Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
        Assert.AreSame(error, result.Exception);
    }

    [TestMethod]
    public async Task DatabaseThatDoesNotAnswerIsUnhealthyWhenTheTimeoutEnds()
    {
        var check = new PostgresHealthCheck(
            new DatabaseStub(async token =>
            {
                await Task.Delay(Timeout.Infinite, token);

                return null!;
            }),
            Settings(check: TimeSpan.FromMilliseconds(200)));

        var result = await check.CheckHealthAsync(Context(), TestContext.CancellationToken).WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);

        Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
        StringAssert.Contains(result.Description, "did not answer");
    }

    [TestMethod]
    public async Task CallerThatGivesUpIsNotReportedAsADatabaseThatIsDown()
    {
        using var cancelled = new CancellationTokenSource();

        var check = new PostgresHealthCheck(
            new DatabaseStub(async token =>
            {
                await Task.Delay(Timeout.Infinite, token);

                return null!;
            }),
            Settings());

        var running = check.CheckHealthAsync(Context(), cancelled.Token);

        await cancelled.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => running);
    }

    [TestMethod]
    public async Task RedisThatAnswersIsHealthy()
    {
        var redis = Environment.GetEnvironmentVariable("OPENWORKER_TEST_REDIS");

        if (string.IsNullOrEmpty(redis))
        {
            Assert.Inconclusive("OPENWORKER_TEST_REDIS is not set.");
        }

        var check = new RedisHealthCheck(new RedisConnectionProvider(redis), Settings());

        var result = await check.CheckHealthAsync(Context(), TestContext.CancellationToken);

        Assert.AreEqual(HealthStatus.Healthy, result.Status);
    }

    /// <summary>
    /// A stand-in for an interface of Redis.OM that answers each call with what the test says.
    /// </summary>
    public class Stub : DispatchProxy
    {
        private Func<MethodInfo, object?> _answer = _ => null;

        public static T Create<T>(Func<MethodInfo, object?> answer) where T : class
        {
            var proxy = Create<T, Stub>();

            ((Stub)(object)proxy)._answer = answer;

            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => _answer(targetMethod!);
    }

    private static IRedisConnectionProvider RedisThatAnswers(Func<Task<RedisReply>> ping)
    {
        var connection = Stub.Create<IRedisConnection>(method => method.Name == nameof(IRedisConnection.ExecuteAsync) ? ping() : null);

        return Stub.Create<IRedisConnectionProvider>(method => method.Name == $"get_{nameof(IRedisConnectionProvider.Connection)}" ? connection : null);
    }

    [TestMethod]
    public async Task RedisThatIsNotThereIsUnhealthyWithTheExceptionNotAnException()
    {
        var error = new InvalidOperationException("connection refused");
        var check = new RedisHealthCheck(RedisThatAnswers(() => Task.FromException<RedisReply>(error)), Settings());

        var result = await check.CheckHealthAsync(Context(), TestContext.CancellationToken);

        Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
        Assert.AreSame(error, result.Exception);
    }

    [TestMethod]
    public async Task RedisThatDoesNotAnswerIsUnhealthyWhenTheTimeoutEnds()
    {
        var check = new RedisHealthCheck(
            RedisThatAnswers(() => new TaskCompletionSource<RedisReply>().Task),
            Settings(check: TimeSpan.FromMilliseconds(200)));

        var result = await check.CheckHealthAsync(Context(), TestContext.CancellationToken).WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);

        Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
        StringAssert.Contains(result.Description, "did not answer");
    }

    [TestMethod]
    public void EachModuleAddsItsCheckOnceEvenWhenItIsAddedTwice()
    {
        var services = new ServiceCollection().AddLogging();

        // Every server calls these once; a test host or a future server may call them again.
        services.AddCache();
        services.AddCache();
        services.AddPersistence();
        services.AddPersistence();
        services.AddServerHealthChecks();
        services.AddServerHealthChecks();

        using var provider = services.BuildServiceProvider();

        var names = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations
            .Select(e => e.Name)
            .Order();

        Assert.AreEqual("listener, postgres, redis, world-loop", string.Join(", ", names));
    }

    [TestMethod]
    public void LoopAndListenerAreChecksOfReadinessAndOnlyTheLoopOfLiveness()
    {
        var services = new ServiceCollection().AddLogging().AddServerHealthChecks();

        using var provider = services.BuildServiceProvider();

        var registrations = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        // A server that drains is alive: liveness would make the orchestrator restart it in the middle of its shutdown.
        Assert.AreEqual("world-loop", string.Join(", ", registrations.Where(e => e.Tags.Contains("live")).Select(e => e.Name).Order()));
        Assert.AreEqual("listener, world-loop", string.Join(", ", registrations.Where(e => e.Tags.Contains("ready")).Select(e => e.Name).Order()));
    }

    public TestContext TestContext { get; set; } = null!;
}
