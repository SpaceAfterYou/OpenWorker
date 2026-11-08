using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Arch.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenWorker.DependencyInjection;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.SoulWorker.Network;

namespace OpenWorker.AuthServer.Test.DependencyInjection;

/// <summary>
/// A server that stops, with real clients on real sockets: it refuses new connections and turns readiness off, the
/// close handlers of every session run on the world loop while the loop still answers, and what was queued for a
/// client reaches it before the connection closes.
/// </summary>
[TestClass]
public sealed class TestGracefulShutdown
{
    // More than the buffers of the sockets hold, so that a good part of it is still queued in the writer when the server stops.
    private const int Frames = 3000;
    private const int FrameSize = 4000;

    private readonly struct Filler(byte value, int size) : IResponseHotspotMessage
    {
        public MessageOpcode Opcode => new(GroupOpcode.Move, MoveOpcode.MoveBt);

        public void Write(BinaryWriter writer)
        {
            var filler = new byte[size + 1];

            Array.Fill(filler, value);

            writer.Write(filler);
        }
    }

    /// <summary>
    /// What happened and in which order, across the loop and the thread pool.
    /// </summary>
    private sealed class Events
    {
        public ConcurrentQueue<string> Log { get; } = new();
    }

    /// <summary>
    /// A close handler that notes what it was run on, and waits for the test when the test says so.
    /// </summary>
    private sealed class RecordingCloseHandler(WorldLoop loop, World world, Events events) : ISessionCloseHandler
    {
        public ConcurrentBag<bool> RanOnTheLoop { get; } = [];
        public ConcurrentBag<bool> EntityWasAlive { get; } = [];

        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource? Gate { get; set; }

        public int Closed => RanOnTheLoop.Count;

        public async ValueTask OnSessionClosedAsync(Entity player)
        {
            events.Log.Enqueue("close");

            RanOnTheLoop.Add(loop.IsCurrent);

            Entered.TrySetResult();

            if (Gate is { } gate)
            {
                await gate.Task;
            }

            // The close of a session ends with its entity: until the last handler is done the entity is there.
            EntityWasAlive.Add(world.IsAlive(player));
        }
    }

    private sealed class RecordingRegistration(Events events, bool fails = false) : IServerRegistration
    {
        public int Calls { get; private set; }

        public Task WithdrawAsync(CancellationToken cancellationToken)
        {
            Calls++;

            events.Log.Enqueue("withdraw");

            return fails ? Task.FromException(new InvalidOperationException("Redis is down.")) : Task.CompletedTask;
        }
    }

    private static Task<ServerHarness> StartAsync(
        Dictionary<string, string?>? settings = null,
        Action<IServiceCollection>? services = null,
        CancellationToken cancellationToken = default)
    {
        return ServerHarness.StartAsync(
            collection =>
            {
                collection.AddSingleton<Events>();
                collection.AddSingleton<RecordingCloseHandler>();
                collection.AddSingleton<ISessionCloseHandler>(e => e.GetRequiredService<RecordingCloseHandler>());

                services?.Invoke(collection);
            },
            settings,
            cancellationToken);
    }

    /// <summary>
    /// Queues frames for every session the server has, the way the handlers of the game do: on the world loop.
    /// </summary>
    private static Task QueueFramesAsync(ServerHarness server, int frames)
    {
        var query = new QueryDescription().WithAll<ServerSessionComponent>();

        return server.Loop.InvokeAsync(() => server.World.Query(in query, (ref ServerSessionComponent session) =>
        {
            for (var frame = 0; frame < frames; frame++)
            {
                session.Send(new Filler(7, FrameSize));
            }
        }));
    }

    /// <summary>
    /// Reads what the server sends until it closes the connection, which a client sees as the end of the stream.
    /// </summary>
    private static async Task<List<(byte Group, byte Command, byte[] Body)>> ReadUntilClosedAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var frames = new List<(byte, byte, byte[])>();

        var stream = client.GetStream();
        var head = new byte[MessageHeader.HeaderSize];

        while (true)
        {
            var read = await stream.ReadAtLeastAsync(head, head.Length, throwOnEndOfStream: false, cancellationToken);

            if (read == 0)
            {
                return frames;
            }

            Assert.AreEqual(head.Length, read, "The connection closed in the middle of a frame header.");

            var header = new MessageHeader(head);
            var payload = new byte[header.ContentLength];

            await stream.ReadExactlyAsync(payload, cancellationToken);

            MessageEncryption.Exchange(payload.AsSpan(), header.Version);

            frames.Add((payload[0], payload[1], payload[2..]));
        }
    }

    /// <summary>
    /// The sessions counted closed so far under one reason, or under all of them.
    /// </summary>
    private static long Closed(MetricCollector<long> closed, string? reason = null)
    {
        return closed.GetMeasurementSnapshot()
            .Where(e => reason is null || (e.Tags.TryGetValue("reason", out var found) && Equals(found, reason)))
            .Sum(e => e.Value);
    }

    /// <summary>
    /// The writer counts its session when its write loop ends, which may be a moment after the stop has returned for a
    /// session that was cut off.
    /// </summary>
    private async Task UntilClosedAsync(MetricCollector<long> closed, long count)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.CancellationToken);

        timeout.CancelAfter(TimeSpan.FromSeconds(10));

        while (Closed(closed) < count)
        {
            await Task.Delay(10, timeout.Token);
        }

        // Long enough for a second count of the same session to show.
        await Task.Delay(100, TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task EverythingQueuedReachesTheClientsBeforeTheConnectionsCloseAndTheHandlersRunForEverySession()
    {
        await using var server = await StartAsync(cancellationToken: TestContext.CancellationToken);

        var handler = server.Host.Services.GetRequiredService<RecordingCloseHandler>();

        using var closed = server.Listen<long>("sessions.closed");
        using var active = server.Listen<long>("sessions.active");

        using var first = await server.ConnectAsync(TestContext.CancellationToken);
        using var second = await server.ConnectAsync(TestContext.CancellationToken);
        using var third = await server.ConnectAsync(TestContext.CancellationToken);

        await QueueFramesAsync(server, Frames);

        // Nobody reads yet: the kernel holds a part and the writers hold the rest, so the stop has frames to flush.
        var stopping = server.StopAsync(TestContext.CancellationToken);

        var received = await Task.WhenAll(
            ReadUntilClosedAsync(first, TestContext.CancellationToken),
            ReadUntilClosedAsync(second, TestContext.CancellationToken),
            ReadUntilClosedAsync(third, TestContext.CancellationToken));

        await stopping;

        foreach (var frames in received)
        {
            Assert.HasCount(Frames + 1, frames);

            Assert.IsTrue(frames.Take(Frames).All(e => e.Group == (byte)GroupOpcode.Move && e.Body.Length == FrameSize + 1 && e.Body.All(b => b == 7)),
                "Every frame that was queued arrives whole and in its place.");

            // The message that the server goes away is the last thing the client gets, then the connection closes.
            var notice = frames[^1];

            Assert.AreEqual((byte)GroupOpcode.Character, notice.Group);
            Assert.AreEqual((byte)CharacterOpcode.KickOut, (byte)(notice.Command & 0x7F));
            Assert.AreEqual((byte)PersonKickOutReason.GmToolServerShutdown, notice.Body[0]);
        }

        Assert.AreEqual(3, handler.Closed);
        Assert.IsTrue(handler.RanOnTheLoop.All(e => e), "The close handlers run on the world loop.");
        Assert.IsTrue(handler.EntityWasAlive.All(e => e), "A close handler finds the entity of its session.");

        Assert.AreEqual(0, await server.CountSessionsAsync(), "The close of a session ends with its entity.");

        // Each drained session is counted once, as closed by the shutdown, and none is left open.
        await UntilClosedAsync(closed, 3);

        Assert.AreEqual(3, Closed(closed));
        Assert.AreEqual(3, Closed(closed, "shutdown"));
        Assert.AreEqual(0, active.GetMeasurementSnapshot().Sum(e => e.Value), "Every session that opened has closed.");
    }

    [TestMethod]
    public async Task ClientThatLeftBeforeTheStopIsCountedOnceAsTheClientAndTheOthersAsTheShutdown()
    {
        await using var server = await StartAsync(cancellationToken: TestContext.CancellationToken);

        using var closed = server.Listen<long>("sessions.closed");

        using var staying = await server.ConnectAsync(TestContext.CancellationToken);
        var leaving = await server.ConnectAsync(TestContext.CancellationToken);

        leaving.Dispose();

        await server.WaitForSessionsAsync(1, TestContext.CancellationToken);
        await UntilClosedAsync(closed, 1);

        await server.StopAsync(TestContext.CancellationToken);

        await UntilClosedAsync(closed, 2);

        Assert.AreEqual(2, Closed(closed));
        Assert.AreEqual(1, Closed(closed, "client"));
        Assert.AreEqual(1, Closed(closed, "shutdown"));
    }

    [TestMethod]
    public async Task WhileTheSessionsCloseTheListenerRefusesReadinessFailsAndTheLoopStillAnswers()
    {
        await using var server = await StartAsync(cancellationToken: TestContext.CancellationToken);

        var handler = server.Host.Services.GetRequiredService<RecordingCloseHandler>();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        handler.Gate = gate;

        using var first = await server.ConnectAsync(TestContext.CancellationToken);
        using var second = await server.ConnectAsync(TestContext.CancellationToken);

        var stopping = server.StopAsync(TestContext.CancellationToken);

        // The drain is inside the close handlers, which wait for the test.
        await handler.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);

        // The listener is closed: a new client is refused, not left waiting.
        var refused = await Assert.ThrowsAsync<SocketException>(async () =>
        {
            using var late = new TcpClient();

            await late.ConnectAsync(IPAddress.Loopback, server.GamePort, TestContext.CancellationToken);
        });

        Assert.AreEqual(SocketError.ConnectionRefused, refused.SocketErrorCode);

        // Readiness is off, liveness holds: the server is going away, not broken.
        using var ready = await server.Http.GetAsync("/readyz", TestContext.CancellationToken);
        using var live = await server.Http.GetAsync("/healthz", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, ready.StatusCode);

        var body = JsonDocument.Parse(await ready.Content.ReadAsStringAsync(TestContext.CancellationToken)).RootElement;

        StringAssert.Contains(body.GetProperty("checks").GetProperty("listener").GetProperty("description").GetString(), "stopping");
        Assert.AreEqual(HttpStatusCode.OK, live.StatusCode);

        // The world loop runs while the close handlers wait: they need it, and so does everything else.
        Assert.IsTrue(await server.Loop.InvokeAsync(() => server.Loop.IsCurrent).WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken));

        Assert.IsFalse(stopping.IsCompleted, "The stop waits for the close handlers.");

        gate.SetResult();

        await stopping;

        Assert.AreEqual(2, handler.Closed);
    }

    [TestMethod]
    public async Task SignalToStopTurnsReadinessOffBeforeAnythingIsStopped()
    {
        await using var server = await StartAsync(cancellationToken: TestContext.CancellationToken);

        using var client = await server.ConnectAsync(TestContext.CancellationToken);

        using var before = await server.Http.GetAsync("/readyz", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, before.StatusCode);

        // What SIGTERM does: the host is told to stop, and has stopped nothing yet.
        server.Host.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();

        using var after = await server.Http.GetAsync("/readyz", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, after.StatusCode);
        Assert.AreEqual(1, await server.CountSessionsAsync(), "The sessions are still open: nothing has stopped.");
    }

    [TestMethod]
    public async Task RecordsAreTakenBackBeforeTheSessionsCloseAndFailureDoesNotStopTheDrain()
    {
        Events? events = null;
        RecordingRegistration? good = null;
        RecordingRegistration? bad = null;

        await using var server = await StartAsync(services: collection =>
        {
            collection.AddSingleton<IServerRegistration>(e =>
            {
                events = e.GetRequiredService<Events>();

                return good = new RecordingRegistration(events);
            });

            collection.AddSingleton<IServerRegistration>(e => bad = new RecordingRegistration(e.GetRequiredService<Events>(), fails: true));
        }, cancellationToken: TestContext.CancellationToken);

        var handler = server.Host.Services.GetRequiredService<RecordingCloseHandler>();

        using var first = await server.ConnectAsync(TestContext.CancellationToken);
        using var second = await server.ConnectAsync(TestContext.CancellationToken);

        await server.StopAsync(TestContext.CancellationToken);

        Assert.IsNotNull(good);
        Assert.IsNotNull(bad);
        Assert.AreEqual(1, good.Calls);
        Assert.AreEqual(1, bad.Calls);

        // The other servers stop sending players here before the first player is sent away; a record that cannot be
        // removed (Redis is down) is no reason to leave the sessions open.
        Assert.IsNotNull(events);
        Assert.AreEqual("withdraw, withdraw, close, close", string.Join(", ", events.Log));
        Assert.AreEqual(2, handler.Closed);
    }

    [TestMethod]
    public async Task ClientThatDoesNotReadIsCutOffWhenTheDrainTimeEnds()
    {
        await using var server = await StartAsync(
            settings: new() { ["Shutdown:DrainTimeout"] = "00:00:01", ["Shutdown:Timeout"] = "00:00:10" },
            cancellationToken: TestContext.CancellationToken);

        var handler = server.Host.Services.GetRequiredService<RecordingCloseHandler>();

        using var closed = server.Listen<long>("sessions.closed");

        // Never read: the writer cannot get rid of what is queued.
        using var silent = await server.ConnectAsync(TestContext.CancellationToken);

        await QueueFramesAsync(server, Frames);

        var timer = System.Diagnostics.Stopwatch.StartNew();

        await server.StopAsync(TestContext.CancellationToken).WaitAsync(TimeSpan.FromSeconds(15), TestContext.CancellationToken);

        Assert.IsLessThan(8000, timer.ElapsedMilliseconds, "A client that does not read holds the stop for the drain time, not for ever.");

        // The session was closed for all that: the handler ran and the entity is gone.
        Assert.AreEqual(1, handler.Closed);
        Assert.AreEqual(0, await server.CountSessionsAsync());

        // Cut off, and still closed by the shutdown: not a client that does not read (backpressure), and once.
        await UntilClosedAsync(closed, 1);

        Assert.AreEqual(1, Closed(closed));
        Assert.AreEqual(1, Closed(closed, "shutdown"));
    }

    [TestMethod]
    public async Task HostWaitsAsLongAsTheSettingsSay()
    {
        await using var server = await StartAsync(
            settings: new() { ["Shutdown:Timeout"] = "00:00:45", ["Shutdown:DrainTimeout"] = "00:00:20" },
            cancellationToken: TestContext.CancellationToken);

        Assert.AreEqual(TimeSpan.FromSeconds(45), server.Host.Services.GetRequiredService<IOptions<HostOptions>>().Value.ShutdownTimeout);
    }

    public TestContext TestContext { get; set; } = null!;
}
