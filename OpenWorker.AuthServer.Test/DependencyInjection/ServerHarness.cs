using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Sockets;
using Arch.Core;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenWorker.DependencyInjection;
using OpenWorker.DependencyInjection.Health;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Telemetry;

namespace OpenWorker.AuthServer.Test.DependencyInjection;

/// <summary>
/// A real host of the world-loop servers: the same registrations <c>Program.cs</c> makes through
/// <c>AddHotspot</c>, the game listener and the health endpoint each on a port the system picks, the database in memory.
/// Redis is not part of it, so a test needs neither Redis nor Postgres.<br/>
/// The listeners are made on port 0 and the harness reads the ports they got. A port picked by the harness and handed to
/// the server later could be taken in between by another test process that runs beside this one.
/// </summary>
internal sealed class ServerHarness : IAsyncDisposable
{
    private static readonly QueryDescription Sessions = new QueryDescription().WithAll<ServerSessionComponent>();

    private Task? _stopping;

    private ServerHarness(IHost host, int gamePort, int healthPort)
    {
        Host = host;
        GamePort = gamePort;
        HealthPort = healthPort;

        Http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{healthPort}") };
    }

    public IHost Host { get; }

    public int GamePort { get; }

    public int HealthPort { get; }

    public HttpClient Http { get; }

    public WorldLoop Loop => Host.Services.GetRequiredService<WorldLoop>();

    public World World => Host.Services.GetRequiredService<World>();

    /// <summary>
    /// Listens to one instrument of the <c>OpenWorker</c> meter of this host, from now on.
    /// </summary>
    public MetricCollector<T> Listen<T>(string instrument) where T : struct
    {
        return new MetricCollector<T>(Host.Services.GetRequiredService<IMeterFactory>(), OpenWorkerMeter.Name, instrument);
    }

    /// <summary>
    /// Starts the host and returns when it listens.
    /// </summary>
    public static async Task<ServerHarness> StartAsync(
        Action<IServiceCollection>? services = null,
        Dictionary<string, string?>? settings = null,
        CancellationToken cancellationToken = default)
    {
        var configuration = new Dictionary<string, string?>
        {
            // Checked at start as in production, which refuses a port of 0 in the settings; the listeners are given 0 below.
            ["Server:Port"] = "1",
            ["Game:Version"] = "1.4.2.3",
            ["Hotspot:PacketDumpPath"] = "",
            ["Hotspot:ErrorBudget:MaxErrors"] = "10",
            ["Hotspot:ErrorBudget:Window"] = "00:01:00",
            ["Health:Port"] = "2",
            ["Health:LoopTimeout"] = "00:00:02",
            ["Health:CheckTimeout"] = "00:00:02",
            ["Shutdown:Timeout"] = "00:00:20",
            ["Shutdown:DrainTimeout"] = "00:00:10",
            // The metrics are recorded for a test that listens to them, and sent nowhere.
            ["Telemetry:Endpoint"] = "",
            ["Telemetry:Protocol"] = "HttpProtobuf",
            ["Telemetry:ExportInterval"] = "00:00:10"
        };

        foreach (var (key, value) in settings ?? [])
        {
            configuration[key] = value;
        }

        var host = new HostBuilder()
            .ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(configuration))
            .UseHealthEndpoint()
            .ConfigureServices((context, collection) =>
            {
                collection.AddTestPersistence();
                collection.AddHotspot(context, EntityComponentService.Auth);

                services?.Invoke(collection);

                ListenOnAnyPort(collection);
            })
            .Build();

        await host.StartAsync(cancellationToken);

        // The accept loop runs on a task of its own: the host has started before the listener does.
        var state = host.Services.GetRequiredService<ServerState>();

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        timeout.CancelAfter(TimeSpan.FromSeconds(10));

        while (!state.IsListening)
        {
            await Task.Delay(5, timeout.Token);
        }

        // Kestrel has bound its port by the time the host has started.
        var health = host.Services.GetRequiredService<IServer>().Features.GetRequiredFeature<IServerAddressesFeature>().Addresses.Single();

        return new ServerHarness(host, state.Port, new Uri(health).Port);
    }

    /// <summary>
    /// Connects a client and waits until the server has made a session for it, so the client counts as connected
    /// from the point of view of the server too.
    /// </summary>
    public async Task<TcpClient> ConnectAsync(CancellationToken cancellationToken = default)
    {
        var before = await CountSessionsAsync();

        var client = new TcpClient();

        await client.ConnectAsync(IPAddress.Loopback, GamePort, cancellationToken);

        await WaitForSessionsAsync(before + 1, cancellationToken);

        return client;
    }

    public Task<int> CountSessionsAsync()
    {
        return Loop.InvokeAsync(() => World.CountEntities(Sessions));
    }

    public async Task WaitForSessionsAsync(int count, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        timeout.CancelAfter(TimeSpan.FromSeconds(10));

        while (await CountSessionsAsync() != count)
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    /// <summary>
    /// Stops the host the way a signal to stop does. Stopping twice is a no-op.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        return _stopping ??= Host.StopAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        Http.Dispose();

        await StopAsync();

        Host.Dispose();
    }

    /// <summary>
    /// The game listener and the health endpoint listen on port 0, so the system gives each a free port when it binds it.
    /// The settings themselves are checked as written: only what the listeners read is replaced.
    /// </summary>
    private static void ListenOnAnyPort(IServiceCollection services)
    {
        services.AddSingleton<IOptions<ServerOptions>>(Options.Create(new ServerOptions { Port = 0 }));

        services.AddSingleton<IOptions<HealthOptions>>(provider =>
        {
            var health = provider.GetRequiredService<IOptionsMonitor<HealthOptions>>().CurrentValue;

            return Options.Create(new HealthOptions { Port = 0, LoopTimeout = health.LoopTimeout, CheckTimeout = health.CheckTimeout });
        });
    }
}
