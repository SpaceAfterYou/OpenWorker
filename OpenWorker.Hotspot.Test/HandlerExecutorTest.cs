using System.Net;
using System.Net.Sockets;
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

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A request on an opcode no real message uses. The first byte says what happens next.
/// </summary>
[HotspotMessage(GroupOpcode.System, ProbeRequest.Command, HotspotMessageDirection.Request)]
public readonly struct ProbeRequest : IRequestHotspotMessage
{
    public const SystemOpcode Command = (SystemOpcode)0x7E;

    public const byte Fine = 0;
    public const byte Fails = 1;
    public const byte ReadsText = 2;
    public const byte Waits = 3;

    public MessageOpcode Opcode => new(GroupOpcode.System, Command);

    public byte Mode { get; }

    public ProbeRequest(BinaryReader reader)
    {
        Mode = reader.ReadByte();

        if (Mode == ReadsText)
        {
            _ = reader.ReadUtf8UnicodeString();
        }
    }
}

public sealed class ProbeHandler : IHotspotHandler<ProbeRequest>
{
    // The tests that use it do not run side by side.
    public static int Handled { get; set; }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, ProbeRequest request)
    {
        Handled++;

        if (request.Mode == ProbeRequest.Fails)
        {
            throw new InvalidOperationException("The handler failed.");
        }

        if (request.Mode == ProbeRequest.Waits)
        {
            await Task.Delay(Timeout.Infinite, context.CancellationToken);
        }
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

    private static readonly HandlerCollection Collection = new(new HandlerCollectionBuilder(new HandlerBuilder(), NullLogger<HandlerCollection>.Instance));

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
            var empty = typeof(HandlerCollection).Assembly.GetType("OpenWorker.Hotspot.Handler.EmptyHandler")!;

            var services = new ServiceCollection().AddSingleton<ProbeHandler>().AddSingleton(empty).BuildServiceProvider();

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
}
