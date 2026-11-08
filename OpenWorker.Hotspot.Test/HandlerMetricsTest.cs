using System.Net;
using System.Net.Sockets;
using Arch.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler;
using OpenWorker.Hotspot.Telemetry;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The metrics of the handlers: how long each takes under its opcode, why packets fail, and the connection that the
/// error budget closes. They share <see cref="ProbeHandler"/>, which counts in a static field, and the registry of Arch.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class HandlerMetricsTest
{
    private static readonly HandlerTable Table = new(new HandlerTableBuilder(new HandlerBuilder(), NullLogger<HandlerTable>.Instance));

    private static readonly MessageOpcode ProbeOpcode = new(GroupOpcode.System, ProbeRequest.Command);

    // A command of the system group that no handler takes.
    private static readonly MessageOpcode NobodysOpcode = new(GroupOpcode.System, (SystemOpcode)0x7D);

    private static byte[] Packet(MessageOpcode opcode, params byte[] rest) => [opcode.Group, opcode.Command, .. rest];

    private sealed class Harness : IDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly TcpClient _server;

        public Harness()
        {
            ProbeHandler.Handled = 0;

            Metrics = new MetricsHost();
            Ecs = World.Create();

            _listener.Start();

            Client = new TcpClient();
            Client.Connect(IPAddress.Loopback, ((IPEndPoint)_listener.LocalEndpoint).Port);

            _server = _listener.AcceptTcpClient();

            var empty = typeof(HandlerTable).Assembly.GetType("OpenWorker.Hotspot.Handler.EmptyHandler")!;
            var services = new ServiceCollection().AddSingleton<ProbeHandler>().AddSingleton(empty).BuildServiceProvider();

            Executor = new HandlerExecutor(
                Table,
                services,
                Ecs,
                new HandlerErrorBudget(new FakeTimeProvider(), Options.Create(new HandlerErrorBudgetOptions { MaxErrors = 10, Window = TimeSpan.FromMinutes(1) })),
                NullLogger<HandlerExecutor>.Instance,
                Metrics.Get<HandlerMetrics>());

            Player = Ecs.Create(new ActorComponent(7, ActorType.User), new HandlerErrorsComponent());
        }

        public MetricsHost Metrics { get; }

        public World Ecs { get; }

        public TcpClient Client { get; }

        public HandlerExecutor Executor { get; }

        public Entity Player { get; }

        /// <summary>
        /// Gives the player its connection, which is counted from here on: listen before it.
        /// </summary>
        public void Connect()
        {
            Ecs.Add(Player, new ServerSessionComponent(_server, Metrics.Get<SessionMetrics>()));
        }

        public ValueTask SendAsync(byte[] packet, CancellationToken cancellationToken = default) =>
            Executor.ExecuteAsync(packet, Player, cancellationToken);

        public void Dispose()
        {
            Client.Dispose();
            _server.Dispose();
            _listener.Stop();
            Ecs.Dispose();
            Metrics.Dispose();
        }
    }

    [TestMethod]
    public async Task AHandlerRecordsItsDurationUnderTheNameOfItsOpcode()
    {
        using var harness = new Harness();
        using var durations = harness.Metrics.Listen<double>("handler.duration");

        await harness.SendAsync(Packet(ProbeOpcode, ProbeRequest.Fine));
        await harness.SendAsync(Packet(ProbeOpcode, ProbeRequest.Fine));

        var recorded = durations.Values("opcode", ProbeOpcode.ToFullString());

        Assert.HasCount(2, recorded);
        Assert.IsTrue(recorded.All(e => e is >= 0 and < 5), "A duration is in seconds.");
    }

    [TestMethod]
    public async Task TheDurationIncludesTheTimeTheHandlerWaits()
    {
        using var harness = new Harness();
        using var durations = harness.Metrics.Listen<double>("handler.duration");

        // The handler waits until the server stops; here it is stopped after 80 ms.
        using var stop = new CancellationTokenSource(TimeSpan.FromMilliseconds(80));

        await harness.SendAsync(Packet(ProbeOpcode, ProbeRequest.Waits), stop.Token);

        var recorded = durations.Values("opcode", ProbeOpcode.ToFullString());

        Assert.HasCount(1, recorded);
        Assert.IsGreaterThanOrEqualTo(0.07, recorded[0]);
    }

    [TestMethod]
    public async Task AnOpcodeWithNoHandlerHasATagOfItsOwn()
    {
        using var harness = new Harness();
        using var durations = harness.Metrics.Listen<double>("handler.duration");

        await harness.SendAsync(Packet(NobodysOpcode));

        Assert.HasCount(1, durations.Values("opcode", "unhandled"));
        Assert.IsEmpty(durations.Values("opcode", NobodysOpcode.ToFullString()), "The opcodes nobody handles share one tag.");
    }

    [TestMethod]
    public async Task AHandlerThatFailsIsCountedUnderItsReasonAndStillTimed()
    {
        using var harness = new Harness();
        using var durations = harness.Metrics.Listen<double>("handler.duration");
        using var errors = harness.Metrics.Listen<long>("handler.errors");

        await harness.SendAsync(Packet(ProbeOpcode, ProbeRequest.Fails));

        Assert.AreEqual(1, errors.Sum());
        Assert.AreEqual(1, errors.Sum("reason", HandlerErrorBudget.Tag(HandlerErrorReason.HandlerFault)));
        Assert.HasCount(1, durations.Values("opcode", ProbeOpcode.ToFullString()));
    }

    [TestMethod]
    public async Task APacketThatCannotBeReadIsMalformedAndHasNoHandlerToTime()
    {
        using var harness = new Harness();
        using var durations = harness.Metrics.Listen<double>("handler.duration");
        using var errors = harness.Metrics.Listen<long>("handler.errors");

        // Shorter than an opcode: nothing to look a handler up by.
        await harness.SendAsync([(byte)GroupOpcode.System]);

        // The opcode is there but the body is not.
        await harness.SendAsync(Packet(ProbeOpcode));

        Assert.AreEqual(2, errors.Sum());
        Assert.AreEqual(2, errors.Sum("reason", HandlerErrorBudget.Tag(HandlerErrorReason.Malformed)));
        Assert.HasCount(1, durations.GetMeasurementSnapshot(), "Only the packet that reached a handler is timed.");
    }

    [TestMethod]
    public async Task ARequestThatSucceedsCountsNoError()
    {
        using var harness = new Harness();
        using var errors = harness.Metrics.Listen<long>("handler.errors");

        await harness.SendAsync(Packet(ProbeOpcode, ProbeRequest.Fine));

        Assert.AreEqual(0, errors.Sum());
    }

    [TestMethod]
    public async Task ASessionThatUsesUpItsBudgetIsClosedAsMalformedOnce()
    {
        using var harness = new Harness();
        using var active = harness.Metrics.Listen<long>("sessions.active");
        using var closed = harness.Metrics.Listen<long>("sessions.closed");

        harness.Connect();

        Assert.AreEqual(1, active.Sum());

        for (var sent = 0; sent < 10; sent++)
        {
            await harness.SendAsync(Packet(ProbeOpcode));
        }

        await MetricAssertions.UntilAsync(() => closed.Sum() > 0, "The session was not counted closed.");

        // The packets still queued are dropped; no later error closes it a second time.
        await harness.SendAsync(Packet(ProbeOpcode));
        await Task.Delay(100);

        Assert.AreEqual(1, closed.Sum());
        Assert.AreEqual(1, closed.Sum("reason", "malformed"));
        Assert.AreEqual(0, active.Sum(), "The session is no longer active once it is closed.");
    }
}
