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
/// A <see cref="ServerService"/> on a port the system picks (its listener is made on port 0), with the handlers of the
/// tests (<see cref="ProbeHandler"/>) and the metrics of its own <see cref="MetricsHost"/>. It creates entities in the registry of Arch, which does not take
/// two threads at once: the tests that use it do not run side by side.
/// </summary>
internal sealed class ServerServiceHost : IAsyncDisposable
{
    private readonly World _ecs = World.Create();
    private readonly WorldLoop _loop = new(NullLogger<WorldLoop>.Instance);
    private readonly ServerService _service;
    private readonly ApplicationLifetime _lifetime = new(NullLogger<ApplicationLifetime>.Instance);
    private readonly ServerState _state;

    public ServerServiceHost()
    {
        ProbeHandler.Handled = 0;

        Metrics = new MetricsHost();

        _state = new ServerState(_lifetime);

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
            Options.Create(new ServerOptions { Port = 0 }),
            Options.Create(new GameOptions { Version = "1.0" }),
            Options.Create(new HotspotOptions()),
            Options.Create(new ShutdownOptions { Timeout = TimeSpan.FromSeconds(30), DrainTimeout = TimeSpan.FromSeconds(15) }),
            new ArchComponentProvider(configuration, NullLogger<ArchComponentProvider>.Instance),
            _state,
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

    public Task StartAsync() => _service.StartAsync(CancellationToken.None);

    public Task StopAsync() => _service.StopAsync(CancellationToken.None);

    /// <summary>
    /// The listener starts a moment after the service does, and its port is known from then on.
    /// </summary>
    public async Task<TcpClient> ConnectAsync()
    {
        await ListeningAsync(_state);

        var client = new TcpClient();

        await client.ConnectAsync(IPAddress.Loopback, _state.Port);

        return client;
    }

    /// <summary>
    /// Waits until the listener of a <see cref="ServerService"/> has started, at most ten seconds.
    /// </summary>
    public static async Task ListeningAsync(ServerState state)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        while (!state.IsListening)
        {
            await Task.Delay(5, timeout.Token);
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
