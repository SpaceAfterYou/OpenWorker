using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.DependencyInjection;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler;
using OpenWorker.Hotspot.SoulWorker.Network;
using OpenWorker.Hotspot.Telemetry;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The lines about a session carry its scope (<see cref="SessionLogScope"/>): the lines of its handlers on the world loop,
/// across their awaits, and the lines of its close handlers, while the other work of the loop carries none.<br/>
/// One factory and one scope provider for every logger, as in a server, so the scope begun by the executor reaches the
/// logger of the handler. The tests create entities and use <see cref="ProbeHandler"/>, so they do not run side by side.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed partial class SessionLogScopeTest
{
    private static readonly HandlerTable Table = new(new HandlerTableBuilder(new HandlerBuilder(), NullLogger<HandlerTable>.Instance));

    /// <summary>
    /// A line the way it was when it was written: the scopes are printed at once, because the scope of a session is one
    /// object that changes as the server learns about the session.
    /// </summary>
    private sealed record Line(string Category, string Message, IReadOnlyList<object?> Scopes, string ScopeText);

    /// <summary>
    /// The loggers of a server: one factory, every line collected with the text of its scopes.
    /// </summary>
    private sealed class Logs : IDisposable
    {
        private readonly ConcurrentQueue<Line> _lines = new();

        public Logs()
        {
            var options = new FakeLogCollectorOptions
            {
                // The sink is called while the line is written: the text of the scope is the one of that moment.
                OutputFormatter = record =>
                {
                    _lines.Enqueue(new Line(record.Category ?? string.Empty, record.Message, record.Scopes,
                        string.Join(" => ", record.Scopes)));

                    return string.Empty;
                },
                OutputSink = _ => { }
            };

            Factory = LoggerFactory.Create(builder => builder
                .SetMinimumLevel(LogLevel.Trace)
                .AddProvider(new FakeLoggerProvider(FakeLogCollector.Create(options))));
        }

        public ILoggerFactory Factory { get; }

        public Line[] Lines => [.. _lines];

        public Line Single(string message) => Lines.Single(e => e.Message == message);

        public void Dispose() => Factory.Dispose();
    }

    /// <summary>
    /// A connected pair of sockets, for a session component of a real connection.
    /// </summary>
    private sealed class Connection : IDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);

        public Connection()
        {
            _listener.Start();

            Client = new TcpClient();
            Client.Connect(IPAddress.Loopback, ((IPEndPoint)_listener.LocalEndpoint).Port);

            Server = _listener.AcceptTcpClient();
        }

        public TcpClient Client { get; }

        public TcpClient Server { get; }

        public void Dispose()
        {
            Client.Dispose();
            Server.Dispose();
            _listener.Stop();
        }
    }

    private sealed class Executor : IDisposable
    {
        public Executor(Logs logs)
        {
            ProbeHandler.Handled = 0;
            ProbeHandler.Gate = null;

            var empty = typeof(HandlerTable).Assembly.GetType("OpenWorker.Hotspot.Handler.EmptyHandler")!;

            var services = new ServiceCollection()
                .AddSingleton(logs.Factory.CreateLogger<ProbeHandler>())
                .AddSingleton<ProbeHandler>()
                .AddSingleton(empty)
                .BuildServiceProvider();

            Loop = new WorldLoop(logs.Factory.CreateLogger<WorldLoop>());

            Handlers = new HandlerExecutor(
                Table,
                services,
                Ecs,
                new HandlerErrorBudget(new FakeTimeProvider(), Options.Create(new HandlerErrorBudgetOptions { MaxErrors = 10, Window = TimeSpan.FromMinutes(1) })),
                logs.Factory.CreateLogger<HandlerExecutor>());

            Player = Ecs.Create();

            Ecs.Add(Player, new ServerSessionComponent(Connection.Server));

            Scope = Ecs.Get<ServerSessionComponent>(Player).LogScope!;

            // The read loop names the entity when it creates it.
            Scope.Session = Player.Id;
        }

        public World Ecs { get; } = World.Create();

        public Connection Connection { get; } = new();

        public WorldLoop Loop { get; }

        public HandlerExecutor Handlers { get; }

        public Entity Player { get; }

        public SessionLogScope Scope { get; }

        public void Dispose()
        {
            ProbeHandler.Gate = null;

            Loop.Dispose();
            Ecs.Dispose();
            Connection.Dispose();
        }
    }

    private static byte[] Packet(byte mode) => [(byte)GroupOpcode.System, (byte)ProbeRequest.Command, mode];

    private static void AssertScopeOf(Line line, SessionLogScope scope)
    {
        Assert.HasCount(1, line.Scopes, $"The line '{line.Message}' must carry the scope of its session and nothing else.");
        Assert.AreSame(scope, line.Scopes[0], "The scope is the one object of the session, not a copy per packet.");
    }

    [TestMethod]
    public async Task TheLinesOfAHandlerCarryTheSessionAcrossItsAwaitAndTheOtherCallbacksDoNot()
    {
        using var logs = new Logs();
        using var executor = new Executor(logs);

        var other = logs.Factory.CreateLogger("Other");
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        ProbeHandler.Gate = gate;

        var handling = executor.Loop.InvokeAsync(() =>
            executor.Handlers.ExecuteAsync(Packet(ProbeRequest.Logs), executor.Player, executor.Scope, TestContext.CancellationToken));

        await MetricAssertions.UntilAsync(() => ProbeHandler.Handled == 1, "The packet did not reach its handler.");

        // The handler waits: the loop runs other work in the meantime, and that work is about no session.
        await executor.Loop.InvokeAsync(() => LogOther(other, "between"));

        gate.SetResult();

        await handling;

        await executor.Loop.InvokeAsync(() => LogOther(other, "later"));

        var before = logs.Single("Probe before the wait.");
        var after = logs.Single("Probe after the wait.");

        AssertScopeOf(before, executor.Scope);
        AssertScopeOf(after, executor.Scope);

        Assert.Contains(typeof(ProbeHandler).FullName!, before.Category, "The line is the handler's own, from its own logger.");

        // The client socket is dual mode and names itself in IPv6; the server sees the IPv4 address.
        var remote = $"127.0.0.1:{((IPEndPoint)executor.Connection.Client.Client.LocalEndPoint!).Port}";

        Assert.AreEqual($"Session={executor.Player.Id} Remote={remote}", before.ScopeText);
        Assert.AreEqual(executor.Player.Id, ScopeValue(before, SessionLogScope.SessionKey));
        Assert.AreEqual(remote, ScopeValue(before, SessionLogScope.RemoteKey));

        Assert.IsEmpty(logs.Single("Other between").Scopes, "The scope of a waiting handler leaked into another callback.");
        Assert.IsEmpty(logs.Single("Other later").Scopes, "The scope of a finished handler leaked into the next callback.");
    }

    [TestMethod]
    public async Task TheAccountAndThePersonAppearOnceTheServerKnowsThem()
    {
        using var logs = new Logs();
        using var executor = new Executor(logs);

        // The overload without a scope finds the one of the session's component.
        await executor.Loop.InvokeAsync(() => executor.Handlers.ExecuteAsync(Packet(ProbeRequest.Logs), executor.Player, TestContext.CancellationToken));

        await executor.Loop.InvokeAsync(() =>
        {
            executor.Ecs.SetLogAccount(executor.Player, 7);
            executor.Ecs.SetLogPerson(executor.Player, 42);
        });

        await executor.Loop.InvokeAsync(() => executor.Handlers.ExecuteAsync(Packet(ProbeRequest.Logs), executor.Player, TestContext.CancellationToken));

        var lines = logs.Lines.Where(e => e.Message == "Probe after the wait.").ToArray();

        Assert.HasCount(2, lines);

        Assert.DoesNotContain("Account=", lines[0].ScopeText);
        Assert.DoesNotContain("Person=", lines[0].ScopeText);
        Assert.EndsWith(" Account=7 Person=42", lines[1].ScopeText);

        AssertScopeOf(lines[1], executor.Scope);
        Assert.AreEqual(7, ScopeValue(lines[1], SessionLogScope.AccountKey));
        Assert.AreEqual(42, ScopeValue(lines[1], SessionLogScope.PersonKey));
    }

    [TestMethod]
    public async Task AnErrorOfTheBudgetCarriesTheScope()
    {
        using var logs = new Logs();
        using var executor = new Executor(logs);

        // The mode byte is missing: the request ends the stream.
        byte[] cut = [(byte)GroupOpcode.System, (byte)ProbeRequest.Command];

        await executor.Loop.InvokeAsync(() => executor.Handlers.ExecuteAsync(cut, executor.Player, executor.Scope, TestContext.CancellationToken));

        var warning = logs.Lines.Single(e => e.Category.EndsWith(nameof(HandlerExecutor), StringComparison.Ordinal));

        AssertScopeOf(warning, executor.Scope);
    }

    [TestMethod]
    public void AnEntityWithoutASessionIsLeftAlone()
    {
        using var world = World.Create();

        var entity = world.Create();

        world.SetLogAccount(entity, 7);
        world.SetLogPerson(entity, 42);

        world.Destroy(entity);

        world.SetLogPerson(entity, 42);

        Assert.IsNull(default(ServerSessionComponent).RemoteEndPoint);
    }

    [TestMethod]
    public void TheTextIsBuiltAgainWhenAValueChanges()
    {
        var scope = new SessionLogScope("10.0.0.1:5000");

        Assert.AreEqual("Remote=10.0.0.1:5000", scope.ToString());

        scope.Session = 3;

        Assert.AreEqual("Session=3 Remote=10.0.0.1:5000", scope.ToString());
        Assert.AreSame(scope.ToString(), scope.ToString(), "The text is kept until a value changes.");

        scope.Person = 9;

        Assert.AreEqual("Session=3 Remote=10.0.0.1:5000 Person=9", scope.ToString());
        Assert.HasCount(4, scope);
        Assert.IsNull(scope[2].Value, "An account not known yet is a null value, not a missing key.");
        Assert.AreEqual(SessionLogScope.PersonKey, scope[3].Key);
    }

    /// <summary>
    /// The close of a session through the server itself: the read loop, the handler on the world loop and the close
    /// handler all write under the scope of the session.
    /// </summary>
    [TestMethod]
    public async Task TheCloseHandlersOfASessionCarryItsScope()
    {
        using var logs = new Logs();

        ProbeHandler.Handled = 0;
        ProbeHandler.Gate = null;

        await using var server = new Server(logs);

        await server.StartAsync();

        using var client = await server.ConnectAsync();

        await client.GetStream().WriteAsync(Frame(Packet(ProbeRequest.Logs)), TestContext.CancellationToken);

        await MetricAssertions.UntilAsync(() => logs.Lines.Any(e => e.Message == "Probe after the wait."), "The packet was not handled.");

        var remote = $"127.0.0.1:{((IPEndPoint)client.Client.LocalEndPoint!).Port}";

        client.Dispose();

        await MetricAssertions.UntilAsync(() => logs.Lines.Any(e => e.Message == CloseHandler.Message), "The close handler did not run.");

        var handled = logs.Single("Probe after the wait.");
        var closed = logs.Single(CloseHandler.Message);

        Assert.HasCount(1, closed.Scopes);
        Assert.AreSame(handled.Scopes[0], closed.Scopes[0], "The close and the packets of a session share its one scope.");
        Assert.IsInstanceOfType<SessionLogScope>(closed.Scopes[0]);
        Assert.StartsWith("Session=", closed.ScopeText);
        Assert.EndsWith($" Remote={remote}", closed.ScopeText);

        // The read loop writes under the same scope, begun on its own task.
        await MetricAssertions.UntilAsync(() => logs.Lines.Any(e => e.Message.StartsWith("Connection closed", StringComparison.Ordinal)), "The read loop did not end.");

        var ended = logs.Lines.First(e => e.Message.StartsWith("Connection closed", StringComparison.Ordinal));

        Assert.AreSame(closed.Scopes[0], ended.Scopes.Single());
    }

    private static object? ScopeValue(Line line, string key) =>
        ((SessionLogScope)line.Scopes[0]!).Single(e => e.Key == key).Value;

    /// <summary>
    /// A packet the way the client sends it: a header, then the encrypted opcode and body.
    /// </summary>
    private static byte[] Frame(byte[] content)
    {
        var header = new MessageHeader(content.Length);
        var body = (byte[])content.Clone();

        MessageEncryption.Exchange(body, header.Version);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        header.ToBinary(writer);
        writer.Write(body);
        writer.Flush();

        return stream.ToArray();
    }

    private sealed partial class CloseHandler(ILogger<CloseHandler> logger) : ISessionCloseHandler
    {
        public const string Message = "The probe session closes.";

        public ValueTask OnSessionClosedAsync(Entity player)
        {
            LogClosing(logger);

            return ValueTask.CompletedTask;
        }

        [LoggerMessage(Level = LogLevel.Information, Message = Message)]
        private static partial void LogClosing(ILogger logger);
    }

    private sealed class Server : IAsyncDisposable
    {
        private readonly World _ecs = World.Create();
        private readonly WorldLoop _loop;
        private readonly ServerService _service;
        private readonly ApplicationLifetime _lifetime = new(NullLogger<ApplicationLifetime>.Instance);
        private readonly MetricsHost _metrics = new();
        private readonly ServerState _state;

        public Server(Logs logs)
        {
            _loop = new WorldLoop(logs.Factory.CreateLogger<WorldLoop>());
            _state = new ServerState(_lifetime);

            var empty = typeof(HandlerTable).Assembly.GetType("OpenWorker.Hotspot.Handler.EmptyHandler")!;

            var services = new ServiceCollection()
                .AddSingleton(logs.Factory.CreateLogger<ProbeHandler>())
                .AddSingleton<ProbeHandler>()
                .AddSingleton(empty)
                .BuildServiceProvider();

            var executor = new HandlerExecutor(
                Table,
                services,
                _ecs,
                new HandlerErrorBudget(new FakeTimeProvider(), Options.Create(new HandlerErrorBudgetOptions { MaxErrors = 10, Window = TimeSpan.FromMinutes(1) })),
                logs.Factory.CreateLogger<HandlerExecutor>());

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { [ConfigurationUtils.InstanceTargetKey] = nameof(EntityComponentService.District) })
                .Build();

            _service = new ServerService(
                _ecs,
                _loop,
                executor,
                Options.Create(new ServerOptions { Port = 0 }),
                Options.Create(new GameOptions { Version = "1.0" }),
                Options.Create(new HotspotOptions()),
                Options.Create(new ShutdownOptions { Timeout = TimeSpan.FromSeconds(30), DrainTimeout = TimeSpan.FromSeconds(15) }),
                new ArchComponentProvider(configuration, NullLogger<ArchComponentProvider>.Instance),
                _state,
                new SessionTracker(),
                [new CloseHandler(logs.Factory.CreateLogger<CloseHandler>())],
                _metrics.Get<SessionMetrics>(),
                TimeProvider.System,
                logs.Factory.CreateLogger<ServerService>());
        }

        public Task StartAsync() => _service.StartAsync(CancellationToken.None);

        /// <summary>
        /// The listener starts a moment after the service does, on a port the system picks.
        /// </summary>
        public async Task<TcpClient> ConnectAsync()
        {
            await ServerServiceHost.ListeningAsync(_state);

            var client = new TcpClient();

            await client.ConnectAsync(IPAddress.Loopback, _state.Port);

            return client;
        }

        public async ValueTask DisposeAsync()
        {
            await _service.StopAsync(CancellationToken.None);

            _service.Dispose();
            _loop.Dispose();
            _ecs.Dispose();
            _metrics.Dispose();
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Other {Name}")]
    private static partial void LogOther(ILogger logger, string name);

    public TestContext TestContext { get; set; } = null!;
}
