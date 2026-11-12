using System.Net;
using System.Net.Sockets;
using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.DependencyInjection;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler;
using OpenWorker.Hotspot.SoulWorker.Network;
using OpenWorker.Hotspot.Telemetry;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A <see cref="ServerService"/> on a free port of the loopback, with the handlers of the tests (<see cref="ProbeHandler"/>)
/// and the metrics of its own <see cref="MetricsHost"/>. It creates entities in the registry of Arch, which does not take
/// two threads at once: the tests that use it do not run side by side.
/// </summary>
internal sealed class ServerServiceHost : IAsyncDisposable
{
    private readonly World _ecs = World.Create();
    private readonly WorldLoop _loop = new(NullLogger<WorldLoop>.Instance);
    private readonly ServerService _service;
    private readonly ApplicationLifetime _lifetime = new(NullLogger<ApplicationLifetime>.Instance);

    public ServerServiceHost()
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
            Options.Create(new ShutdownOptions { Timeout = TimeSpan.FromSeconds(30), DrainTimeout = TimeSpan.FromSeconds(15) }),
            new ArchComponentProvider(configuration, NullLogger<ArchComponentProvider>.Instance),
            new ServerState(_lifetime),
            new SessionTracker(),
            [],
            Metrics.Get<SessionMetrics>(),
            Time,
            NullLogger<ServerService>.Instance);
    }

    public MetricsHost Metrics { get; }

    /// <summary>
    /// The clock of the server: nothing that waits on it happens until a test moves it on.
    /// </summary>
    public FakeTimeProvider Time { get; } = new();

    /// <summary>
    /// Every connection that has its entity now enters, as a login does: it gets the account it logs in with. Returns how
    /// many entered.
    /// </summary>
    public Task<int> EnterAllAsync(int account)
    {
        return _loop.InvokeAsync(() =>
        {
            var sessions = new QueryDescription().WithAll<ServerSessionComponent, ClaimsComponent>();
            var entered = new List<Entity>();

            _ecs.Query(in sessions, entered.Add);

            foreach (var entity in entered)
            {
                _ecs.Set(entity, new ClaimsComponent(account, 1));
            }

            return entered.Count;
        });
    }

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

    /// <summary>
    /// A packet the way the client sends it: a header, then the encrypted opcode and body.
    /// </summary>
    public static byte[] Frame(params byte[] content)
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
}
