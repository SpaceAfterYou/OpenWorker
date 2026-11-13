using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Handler.Extensions;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A request on an opcode no real message uses. The first byte says what happens next.
/// </summary>
[HotspotMessage(GroupOpcode.System, (byte)ProbeRequest.Command, HotspotMessageDirection.Request)]
public readonly struct ProbeRequest : IRequestHotspotMessage, IWireReadable<ProbeRequest>
{
    public const SystemOpcode Command = (SystemOpcode)0x7E;

    public const byte Fine = 0;
    public const byte Fails = 1;
    public const byte ReadsText = 2;
    public const byte Waits = 3;
    public const byte Logs = 4;
    public const byte Records = 5;

    public MessageOpcode Opcode => new(GroupOpcode.System, (byte)Command);

    public byte Mode { get; private init; }

    /// <summary>
    /// What follows the mode, for <see cref="Records"/>.
    /// </summary>
    public byte[] Rest { get; private init; }

    public static ProbeRequest Read(ref PacketReader reader)
    {
        var mode = reader.ReadByte();

        if (mode == ReadsText)
        {
            _ = reader.ReadUtf8UnicodeString();
        }

        return new ProbeRequest { Mode = mode, Rest = mode == Records ? reader.ReadByteArray(reader.Remaining) : [] };
    }
}

/// <param name="logger">Where <see cref="ProbeRequest.Logs"/> writes; only the tests of the log scope give one.</param>
public sealed partial class ProbeHandler(ILogger<ProbeHandler>? logger = null) : IHotspotHandler<ProbeRequest>
{
    // The tests that use it do not run side by side.
    public static int Handled { get; set; }

    /// <summary>
    /// What <see cref="ProbeRequest.Logs"/> waits for between its two lines; <see langword="null"/> to wait for nothing
    /// but a round through the loop.
    /// </summary>
    public static TaskCompletionSource? Gate { get; set; }

    /// <summary>
    /// What the requests of <see cref="ProbeRequest.Records"/> carried after the mode, in the order they were handled.
    /// </summary>
    public static ConcurrentQueue<byte[]> Recorded { get; } = new();

    public async ValueTask OnHandleAsync(ServiceHandleContext context, ProbeRequest request)
    {
        Handled++;

        if (request.Mode == ProbeRequest.Records)
        {
            Recorded.Enqueue(request.Rest);
        }

        if (request.Mode == ProbeRequest.Logs && logger is { } log)
        {
            LogBefore(log);

            // Off the loop and back: the line after the wait is written by a continuation of its own.
            if (Gate is { } gate)
            {
                await gate.Task;
            }

            else
            {
                await Task.Delay(1, context.CancellationToken);
            }

            LogAfter(log);
        }

        if (request.Mode == ProbeRequest.Fails)
        {
            throw new InvalidOperationException("The handler failed.");
        }

        if (request.Mode == ProbeRequest.Waits)
        {
            await Task.Delay(Timeout.Infinite, context.CancellationToken);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Probe before the wait.")]
    private static partial void LogBefore(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Probe after the wait.")]
    private static partial void LogAfter(ILogger logger);
}

/// <summary>
/// A request with a span format, on an opcode no real message uses: a list of numbers with an int count.
/// </summary>
[HotspotMessage(GroupOpcode.System, (byte)WireProbeRequest.Command, HotspotMessageDirection.Request)]
public readonly struct WireProbeRequest : IRequestHotspotMessage, IWireReadable<WireProbeRequest>
{
    public const SystemOpcode Command = (SystemOpcode)0x7C;

    public MessageOpcode Opcode => new(GroupOpcode.System, (byte)Command);

    public int[] Values { get; init; }

    public static WireProbeRequest Read(ref PacketReader reader)
    {
        return new WireProbeRequest { Values = reader.ReadInt32Array(reader.ReadLength(WireLength.Signed32, sizeof(int))) };
    }
}

public sealed class WireProbeHandler : IHotspotHandler<WireProbeRequest>
{
    // The tests that use it do not run side by side.
    public static int[]? Last { get; set; }

    public ValueTask OnHandleAsync(ServiceHandleContext context, WireProbeRequest request)
    {
        Last = request.Values;

        return ValueTask.CompletedTask;
    }
}

internal sealed record LogEntry(LogLevel Level, string Message, Exception? Exception, IReadOnlyDictionary<string, object?> Values);

internal sealed class CapturingLogger<T> : ILogger<T>
{
    public List<LogEntry> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var values = new Dictionary<string, object?>();

        if (state is IEnumerable<KeyValuePair<string, object?>> pairs)
        {
            foreach (var (key, value) in pairs)
            {
                values[key] = value;
            }
        }

        Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception, values));
    }
}

/// <summary>
/// Creating entities registers component types in a registry that does not take two threads at once.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class HandlerExecutorTest
{
    private static readonly TimeSpan Minute = TimeSpan.FromMinutes(1);

    private static readonly HandlerTable Collection = new(new HandlerTableBuilder(new HandlerBuilder(), NullLogger<HandlerTable>.Instance));

    private static readonly byte[] OpcodeBytes = [(byte)GroupOpcode.System, (byte)ProbeRequest.Command];

    // The mode byte is missing: the request ends the stream.
    private static readonly byte[] Cut = OpcodeBytes;

    private static byte[] Packet(byte mode, params byte[] rest) => [.. OpcodeBytes, mode, .. rest];

    private sealed class Harness : IDisposable
    {
        public const int Person = 7;

        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly TcpClient _server;

        public Harness(bool withErrors = true, bool withSession = true)
        {
            ProbeHandler.Handled = 0;

            _listener.Start();

            Client = new TcpClient();
            Client.Connect(IPAddress.Loopback, ((IPEndPoint)_listener.LocalEndpoint).Port);

            _server = _listener.AcceptTcpClient();

            Ecs = World.Create();
            Time = new FakeTimeProvider();
            Logger = new CapturingLogger<HandlerExecutor>();

            // The slot of an opcode nobody handles holds the empty handler, which the container has to know.
            var empty = typeof(HandlerTable).Assembly.GetType("OpenWorker.Hotspot.Handler.EmptyHandler")!;

            var services = new ServiceCollection()
                .AddSingleton<ProbeHandler>()
                .AddSingleton<WireProbeHandler>()
                .AddSingleton(empty)
                .BuildServiceProvider();

            Executor = new HandlerExecutor(
                Collection,
                services,
                Ecs,
                new HandlerErrorBudget(Time, Options.Create(new HandlerErrorBudgetOptions { MaxErrors = 10, Window = Minute })),
                Logger);

            Player = Ecs.Create(new ActorComponent(Person, ActorType.User));

            if (withSession)
            {
                Ecs.Add(Player, new ServerSessionComponent(_server));
            }

            if (withErrors)
            {
                Ecs.Add(Player, new HandlerErrorsComponent());
            }
        }

        public TcpClient Client { get; }

        public World Ecs { get; }

        public FakeTimeProvider Time { get; }

        public CapturingLogger<HandlerExecutor> Logger { get; }

        public HandlerExecutor Executor { get; }

        public Entity Player { get; }

        public HandlerErrorsComponent Errors => Ecs.Get<HandlerErrorsComponent>(Player);

        /// <summary>
        /// Every line about a failed packet, whatever its level.
        /// </summary>
        public LogEntry[] Failures => Logger.Entries.Where(e => e.Values.ContainsKey("Reason")).ToArray();

        public LogEntry[] Warnings => Logger.Entries.Where(e => e.Level == LogLevel.Warning).ToArray();

        public ValueTask SendAsync(byte[] packet, CancellationToken cancellationToken = default) =>
            Executor.ExecuteAsync(packet, Player, cancellationToken);

        public async Task SendManyAsync(int count, byte[] packet)
        {
            for (var sent = 0; sent < count; sent++)
            {
                await SendAsync(packet);
            }
        }

        /// <summary>
        /// The server closed the connection: nothing is ever sent here, so a readable socket has reached its end.
        /// </summary>
        public bool WasClosed(int waitMilliseconds) => Client.Client.Poll(waitMilliseconds * 1000, SelectMode.SelectRead);

        public void Dispose()
        {
            Client.Dispose();
            _server.Dispose();
            _listener.Stop();
            Ecs.Dispose();
        }
    }

    [TestMethod]
    public async Task TheTenthMalformedPacketInAMinuteClosesTheSession()
    {
        using var harness = new Harness();

        await harness.SendManyAsync(9, Cut);

        Assert.AreEqual(9, harness.Errors.Count);
        Assert.IsFalse(harness.Errors.Closed);
        Assert.IsFalse(harness.WasClosed(300), "Nine bad packets are within the budget.");

        await harness.SendAsync(Cut);

        Assert.IsTrue(harness.Errors.Closed);
        Assert.IsTrue(harness.WasClosed(5000), "The tenth bad packet must close the connection.");
    }

    [TestMethod]
    public async Task TheLogHoldsTheFirstErrorAndTheClosingOnly()
    {
        using var harness = new Harness();

        await harness.SendManyAsync(10, Cut);

        Assert.IsFalse(harness.Logger.Entries.Any(e => e.Level >= LogLevel.Error), "A bad packet is no Critical or Error.");

        var warnings = harness.Warnings;

        Assert.HasCount(2, warnings);

        var first = warnings[0];

        Assert.IsInstanceOfType<EndOfStreamException>(first.Exception);
        Assert.AreEqual("malformed", first.Values["Reason"]);
        Assert.AreEqual(1, first.Values["Count"]);
        Assert.AreEqual(10, first.Values["Limit"]);
        Assert.AreEqual("System:126", first.Values["Opcode"]?.ToString());
        Assert.AreEqual(Harness.Person, first.Values["Actor"]);
        Assert.IsTrue(first.Values.ContainsKey("Entity"));
        Assert.StartsWith("127.0.0.1:", first.Values["Remote"]?.ToString());

        var closing = warnings[1];

        Assert.AreEqual(10, closing.Values["Count"]);
        Assert.AreEqual("malformed", closing.Values["Reason"]);
        Assert.Contains("Closing session", closing.Message);
        Assert.Contains("10 failed packets", closing.Message);

        // The ones between are counted and told apart from the warnings.
        Assert.AreEqual(8, harness.Logger.Entries.Count(e => e.Level == LogLevel.Debug && e.Message.Contains("another packet", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task TheHandlerFaultsCountLikeTheMalformedPackets()
    {
        using var harness = new Harness();

        await harness.SendManyAsync(5, Cut);
        await harness.SendManyAsync(5, Packet(ProbeRequest.Fails));

        Assert.IsTrue(harness.Errors.Closed);
        Assert.IsTrue(harness.WasClosed(5000));

        var warnings = harness.Warnings;

        Assert.AreEqual("malformed", warnings[0].Values["Reason"]);
        Assert.AreEqual("handler_fault", warnings[1].Values["Reason"]);
        Assert.IsInstanceOfType<InvalidOperationException>(warnings[1].Exception);
        Assert.AreEqual(5, ProbeHandler.Handled);
    }

    [TestMethod]
    public async Task ALoneHandlerFaultIsAWarningNotCritical()
    {
        using var harness = new Harness();

        await harness.SendAsync(Packet(ProbeRequest.Fails));

        var entry = harness.Warnings.Single();

        Assert.AreEqual(LogLevel.Warning, entry.Level);
        Assert.AreEqual("handler_fault", entry.Values["Reason"]);
        Assert.AreEqual(1, harness.Errors.Count);
        Assert.IsFalse(harness.WasClosed(200));
    }

    [TestMethod]
    public async Task TheCountStartsOverWhenAMinuteHasPassed()
    {
        using var harness = new Harness();

        await harness.SendManyAsync(9, Cut);

        harness.Time.Advance(Minute);

        await harness.SendManyAsync(9, Cut);

        Assert.AreEqual(9, harness.Errors.Count);
        Assert.IsFalse(harness.Errors.Closed);
        Assert.IsFalse(harness.WasClosed(300));

        // One window logs its first error: two warnings for two windows.
        Assert.HasCount(2, harness.Warnings);

        await harness.SendAsync(Cut);

        Assert.IsTrue(harness.Errors.Closed);
        Assert.IsTrue(harness.WasClosed(5000));
    }

    [TestMethod]
    public async Task GoodPacketsAreNotCounted()
    {
        using var harness = new Harness();

        await harness.SendManyAsync(100, Packet(ProbeRequest.Fine));
        await harness.SendManyAsync(9, Cut);

        Assert.AreEqual(100, ProbeHandler.Handled);
        Assert.AreEqual(9, harness.Errors.Count);
        Assert.IsFalse(harness.WasClosed(300));
    }

    [TestMethod]
    public async Task APacketShorterThanTheOpcodeDoesNotThrowOutOfTheExecutor()
    {
        using var harness = new Harness();

        await harness.SendAsync([]);
        await harness.SendAsync([(byte)GroupOpcode.System]);

        Assert.AreEqual(2, harness.Errors.Count);
        Assert.AreEqual("none", harness.Failures[0].Values["Opcode"]?.ToString());
        Assert.AreEqual("none", harness.Failures[1].Values["Opcode"]?.ToString());
        Assert.AreEqual("malformed", harness.Failures[0].Values["Reason"]);
    }

    [TestMethod]
    public async Task AnOpcodeOutsideTheTableIsMalformed()
    {
        using var harness = new Harness();

        // The command byte from 0x80 up makes the two bytes a negative short.
        await harness.SendAsync([(byte)GroupOpcode.System, 0xFF]);
        await harness.SendAsync([0xFF, 0x7F]);

        var failures = harness.Failures;

        Assert.AreEqual(2, harness.Errors.Count);
        Assert.HasCount(2, failures);
        Assert.IsTrue(failures.All(e => e.Values["Reason"]?.ToString() == "malformed"));
        Assert.IsInstanceOfType<InvalidDataException>(failures[0].Exception);

        // The name of a group nobody knows must not fail the log line either.
        Assert.AreEqual("System:255", failures[0].Values["Opcode"]?.ToString());
        Assert.AreEqual("255:0x7F", failures[1].Values["Opcode"]?.ToString());
    }

    [TestMethod]
    public async Task AnUnknownGroupInsideTheTableIsNotAnError()
    {
        using var harness = new Harness();

        // Nothing handles it: the empty handler takes it, as it did before.
        await harness.SendAsync([0xF0, 0x01]);

        Assert.AreEqual(0, harness.Errors.Count);
        Assert.IsEmpty(harness.Logger.Entries.Where(e => e.Level >= LogLevel.Warning));
    }

    [TestMethod]
    public async Task ANegativeStringLengthIsMalformed()
    {
        using var harness = new Harness();

        await harness.SendAsync(Packet(ProbeRequest.ReadsText, 0xFF, 0xFF, 0x41, 0x00));

        var entry = harness.Warnings.Single();

        Assert.IsInstanceOfType<InvalidDataException>(entry.Exception);
        Assert.AreEqual("malformed", entry.Values["Reason"]);
        Assert.AreEqual(0, ProbeHandler.Handled, "The handler must not run on a request that did not parse.");
    }

    /// <summary>
    /// A request is parsed from the span of the packet, whole, before its handler runs. A count it cannot hold is malformed
    /// and counted like any other, and the handler does not run.
    /// </summary>
    [TestMethod]
    public async Task ARequestWithASpanFormatIsParsedFromThePacketAndAHostileCountIsMalformed()
    {
        using var harness = new Harness();

        byte[] opcode = [(byte)GroupOpcode.System, (byte)WireProbeRequest.Command];

        WireProbeHandler.Last = null;

        await harness.SendAsync([.. opcode, 0x02, 0x00, 0x00, 0x00, 0x07, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00]);

        int[] values = [7, 8];

        CollectionAssert.AreEqual(values, WireProbeHandler.Last);
        Assert.AreEqual(0, harness.Errors.Count);

        WireProbeHandler.Last = null;

        await harness.SendAsync([.. opcode, 0xFF, 0xFF, 0xFF, 0x7F, 0x07, 0x00, 0x00, 0x00]);

        var entry = harness.Warnings.Single();

        Assert.IsInstanceOfType<InvalidDataException>(entry.Exception);
        Assert.AreEqual("malformed", entry.Values["Reason"]);
        Assert.AreEqual(1, harness.Errors.Count);
        Assert.IsNull(WireProbeHandler.Last, "The handler must not run on a request that did not parse.");
    }

    [TestMethod]
    public async Task APacketAfterTheCloseIsDropped()
    {
        using var harness = new Harness();

        await harness.SendManyAsync(10, Cut);

        var logged = harness.Logger.Entries.Count;

        await harness.SendAsync(Packet(ProbeRequest.Fine));
        await harness.SendAsync(Cut);

        Assert.AreEqual(0, ProbeHandler.Handled);
        Assert.AreEqual(10, harness.Errors.Count);
        Assert.HasCount(logged, harness.Logger.Entries);
    }

    [TestMethod]
    public async Task AStoppingServerIsNotAnError()
    {
        using var harness = new Harness();
        using var stopping = new CancellationTokenSource();

        var running = harness.SendAsync(Packet(ProbeRequest.Waits), stopping.Token);

        await stopping.CancelAsync();
        await running;

        Assert.AreEqual(0, harness.Errors.Count);
        Assert.IsEmpty(harness.Logger.Entries);
    }

    [TestMethod]
    public async Task ASessionWithoutTheComponentGetsOneWhenItFails()
    {
        using var harness = new Harness(withErrors: false);

        await harness.SendAsync(Cut);

        Assert.AreEqual(1, harness.Errors.Count);
    }

    [TestMethod]
    public async Task AnEntityWithoutASessionStillCountsAndNothingBreaks()
    {
        using var harness = new Harness(withSession: false);

        await harness.SendManyAsync(10, Cut);

        Assert.IsTrue(harness.Errors.Closed);
    }

    [TestMethod]
    public async Task AnErrorAfterTheEntityIsGoneChargesNobody()
    {
        using var harness = new Harness();

        harness.Ecs.Destroy(harness.Player);

        await harness.SendAsync(Cut);

        Assert.IsEmpty(harness.Logger.Entries.Where(e => e.Level >= LogLevel.Warning));
    }

    [TestMethod]
    public void TheContainerBuildsTheExecutorWithTheBudgetOfTheConfiguration()
    {
        using var world = World.Create();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hotspot:ErrorBudget:MaxErrors"] = "7",
                ["Hotspot:ErrorBudget:Window"] = "00:02:30"
            })
            .Build();

        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton(world)
            .AddHotspotHandlers()
            .BuildServiceProvider();

        var budget = services.GetRequiredService<HandlerErrorBudget>();

        Assert.IsNotNull(services.GetRequiredService<HandlerExecutor>());
        Assert.AreSame(TimeProvider.System, services.GetRequiredService<TimeProvider>());
        Assert.AreEqual(7, budget.Options.MaxErrors);
        Assert.AreEqual(TimeSpan.FromSeconds(150), budget.Options.Window);
    }

    /// <summary>
    /// Counts what is asked of the container.
    /// </summary>
    private sealed class CountingProvider(IServiceProvider inner) : IServiceProvider
    {
        public int Calls { get; private set; }

        public object? GetService(Type serviceType)
        {
            Calls++;

            return inner.GetService(serviceType);
        }
    }

    [TestMethod]
    public async Task APacketFindsItsHandlerOnceAndAllocatesNothingAfterwards()
    {
        using var world = World.Create();

        var empty = typeof(HandlerTable).Assembly.GetType("OpenWorker.Hotspot.Handler.EmptyHandler")!;

        await using var services = new ServiceCollection().AddSingleton<ProbeHandler>().AddSingleton(empty).BuildServiceProvider();

        var provider = new CountingProvider(services);

        var executor = new HandlerExecutor(
            Collection,
            provider,
            world,
            new HandlerErrorBudget(TimeProvider.System, Options.Create(new HandlerErrorBudgetOptions { MaxErrors = 10, Window = Minute })),
            NullLogger<HandlerExecutor>.Instance);

        var player = world.Create();
        var packet = Packet(ProbeRequest.Fine);

        ProbeHandler.Handled = 0;

        // The first packet resolves the handler, and the code of the path is compiled.
        await executor.ExecuteAsync(packet, player, null, CancellationToken.None);

        const int Packets = 10_000;

        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var sent = 0; sent < Packets; sent++)
        {
            // The handler completes at once, as most do, so the dispatch never leaves this thread.
            var dispatch = executor.ExecuteAsync(packet, player, null, CancellationToken.None);

            Assert.IsTrue(dispatch.IsCompletedSuccessfully);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(Packets + 1, ProbeHandler.Handled);
        Assert.AreEqual(1, provider.Calls, "The handler is resolved once, by the first packet, and kept.");

        // A debug build makes a class of every async method, one allocation per call: only an optimized build, the one CI
        // tests, can show that the dispatch allocates nothing.
        if (typeof(HandlerExecutor).Assembly.GetCustomAttribute<System.Diagnostics.DebuggableAttribute>() is { IsJITOptimizerDisabled: true })
        {
            Assert.Inconclusive($"{allocated} bytes for {Packets} packets in a debug build; run the tests in Release to measure them.");
        }

        // Nothing per packet: a few bytes in all are left for what the runtime may allocate once on this thread.
        Assert.IsLessThan(Packets, allocated, $"{allocated} bytes for {Packets} packets.");
    }

    /// <summary>
    /// A session that sends the packets of a group faster than its limit loses the packets over it: they are dropped
    /// before their handler, counted under their group, and are no error of the session. The bucket fills again with time.
    /// </summary>
    [TestMethod]
    public async Task APacketOverTheRateOfItsGroupIsDroppedAndCountedWithoutAnError()
    {
        using var metrics = new MetricsHost();
        using var refused = metrics.Listen<long>("handler.refused");
        using var world = World.Create();

        var time = new FakeTimeProvider();
        var empty = typeof(HandlerTable).Assembly.GetType("OpenWorker.Hotspot.Handler.EmptyHandler")!;

        await using var services = new ServiceCollection().AddSingleton<ProbeHandler>().AddSingleton(empty).BuildServiceProvider();

        // The probe is of the system group, which falls under the other packets.
        var limits = new PacketRateLimitOptions { Other = new PacketRateLimit { PerSecond = 2, Burst = 3 } };

        var executor = new HandlerExecutor(
            Collection,
            services,
            world,
            new HandlerErrorBudget(time, Options.Create(new HandlerErrorBudgetOptions { MaxErrors = 10, Window = Minute })),
            NullLogger<HandlerExecutor>.Instance,
            metrics.Get<Telemetry.HandlerMetrics>(),
            new PacketRateLimiter(time, Options.Create(limits)));

        var player = world.Create(new ActorComponent(7, ActorType.User), new HandlerErrorsComponent());

        ProbeHandler.Handled = 0;

        for (var sent = 0; sent < 5; sent++)
        {
            await executor.ExecuteAsync(Packet(ProbeRequest.Fine), player, null, CancellationToken.None);
        }

        Assert.AreEqual(3, ProbeHandler.Handled, "The burst of three goes through, the two after it do not.");
        Assert.AreEqual(2, refused.Sum("group", "other"));
        Assert.AreEqual(0, world.Get<HandlerErrorsComponent>(player).Count, "A dropped packet is no error of the session.");

        time.Advance(TimeSpan.FromSeconds(1));

        for (var sent = 0; sent < 3; sent++)
        {
            await executor.ExecuteAsync(Packet(ProbeRequest.Fine), player, null, CancellationToken.None);
        }

        Assert.AreEqual(5, ProbeHandler.Handled, "A second later two more fit.");
        Assert.AreEqual(3, refused.Sum("group", "other"));
    }
}
