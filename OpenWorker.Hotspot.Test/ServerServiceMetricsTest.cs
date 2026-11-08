using System.Net;
using System.Net.Sockets;
using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
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
/// The accept and read loops of a server over a real socket: what they count, and why a connection that ends by itself ends.
/// They create entities in the registry of Arch, which does not take two threads at once, and use <see cref="ProbeHandler"/>.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class ServerServiceMetricsTest
{
    private sealed class Server : IAsyncDisposable
    {
        private readonly World _ecs = World.Create();
        private readonly WorldLoop _loop = new(NullLogger<WorldLoop>.Instance);
        private readonly ServerService _service;

        public Server()
        {
            ProbeHandler.Handled = 0;

            Metrics = new MetricsHost();

            Port = FreePort();

            var empty = typeof(HandlerTable).Assembly.GetType("OpenWorker.Hotspot.Handler.EmptyHandler")!;
            var services = new ServiceCollection().AddSingleton<ProbeHandler>().AddSingleton(empty).BuildServiceProvider();

            var executor = new HandlerExecutor(
                new HandlerTable(new HandlerTableBuilder(new HandlerBuilder(), NullLogger<HandlerTable>.Instance)),
                services,
                _ecs,
                new HandlerErrorBudget(new FakeTimeProvider(), Options.Create(new HandlerErrorBudgetOptions { MaxErrors = 10, Window = TimeSpan.FromMinutes(1) })),
                NullLogger<HandlerExecutor>.Instance,
                Metrics.Get<HandlerMetrics>());

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { [ConfigurationUtils.InstanceTargetKey] = nameof(EntityComponentService.District) })
                .Build();

            _service = new ServerService(
                _ecs,
                _loop,
                executor,
                Options.Create(new ServerOptions { Port = Port }),
                Options.Create(new GameOptions { Version = "1.0" }),
                Options.Create(new HotspotOptions()),
                new ArchComponentProvider(configuration, NullLogger<ArchComponentProvider>.Instance),
                [],
                Metrics.Get<SessionMetrics>(),
                NullLogger<ServerService>.Instance);
        }

        public MetricsHost Metrics { get; }

        public int Port { get; }

        public Task StartAsync() => _service.StartAsync(CancellationToken.None);

        public Task StopAsync() => _service.StopAsync(CancellationToken.None);

        /// <summary>
        /// The listener starts a moment after the service does.
        /// </summary>
        public async Task<TcpClient> ConnectAsync()
        {
            for (var attempt = 0; ; attempt++)
            {
                var client = new TcpClient();

                try
                {
                    await client.ConnectAsync(IPAddress.Loopback, Port);

                    return client;
                }

                catch (SocketException) when (attempt < 100)
                {
                    client.Dispose();

                    await Task.Delay(50);
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _service.StopAsync(CancellationToken.None);

            _service.Dispose();
            _loop.Dispose();
            _ecs.Dispose();
            Metrics.Dispose();
        }

        private static int FreePort()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);

            probe.Start();

            var port = ((IPEndPoint)probe.LocalEndpoint).Port;

            probe.Stop();

            return port;
        }
    }

    /// <summary>
    /// A packet the way the client sends it: a header, then the encrypted opcode and body.
    /// </summary>
    private static byte[] Frame(params byte[] content)
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

    [TestMethod]
    public async Task AClientThatLeavesIsCountedAsTheClientAndWhatItSentIsCounted()
    {
        await using var server = new Server();
        using var active = server.Metrics.Listen<long>("sessions.active");
        using var closed = server.Metrics.Listen<long>("sessions.closed");
        using var received = server.Metrics.Listen<long>("bytes.received");

        await server.StartAsync();

        using var client = await server.ConnectAsync();

        await MetricAssertions.UntilAsync(() => active.Sum() == 1, "The connection was not counted.");

        var frame = Frame((byte)GroupOpcode.System, (byte)ProbeRequest.Command, ProbeRequest.Fine);

        await client.GetStream().WriteAsync(frame);

        await MetricAssertions.UntilAsync(() => ProbeHandler.Handled == 1, "The packet did not reach its handler.");

        Assert.AreEqual(frame.Length, received.Sum(), "The header counts.");

        client.Dispose();

        await MetricAssertions.UntilAsync(() => closed.Sum() > 0, "A client that left was not counted.");

        Assert.AreEqual(1, closed.Sum("reason", "client"));
        Assert.AreEqual(1, closed.Sum());
        await MetricAssertions.UntilAsync(() => active.Sum() == 0, "The connection stayed active after it closed.");
    }

    [TestMethod]
    public async Task AClientThatSendsTooManyBadPacketsIsCountedAsMalformedNotAsTheClient()
    {
        await using var server = new Server();
        using var closed = server.Metrics.Listen<long>("sessions.closed");
        using var errors = server.Metrics.Listen<long>("handler.errors");

        await server.StartAsync();

        using var client = await server.ConnectAsync();

        // The mode byte is missing: the request ends the stream.
        var bad = Frame((byte)GroupOpcode.System, (byte)ProbeRequest.Command);

        for (var sent = 0; sent < 10; sent++)
        {
            await client.GetStream().WriteAsync(bad);
        }

        await MetricAssertions.UntilAsync(() => closed.Sum() > 0, "The server did not close the connection.");

        // The client sees the close and its socket reports the end of the stream, which must not become a second count.
        client.Dispose();
        await Task.Delay(200);

        Assert.AreEqual(1, closed.Sum());
        Assert.AreEqual(1, closed.Sum("reason", "malformed"));
        Assert.AreEqual(10, errors.Sum("reason", "malformed"));
    }

    [TestMethod]
    public async Task AConnectionThatTheServerEndsByStoppingIsTheServers()
    {
        await using var server = new Server();
        using var active = server.Metrics.Listen<long>("sessions.active");
        using var closed = server.Metrics.Listen<long>("sessions.closed");

        await server.StartAsync();

        using var client = await server.ConnectAsync();

        await MetricAssertions.UntilAsync(() => active.Sum() == 1, "The connection was not counted.");

        await server.StopAsync();

        await MetricAssertions.UntilAsync(() => closed.Sum() > 0, "The connection was not counted closed when the server stopped.");

        Assert.AreEqual(1, closed.Sum("reason", "server"));
    }
}
