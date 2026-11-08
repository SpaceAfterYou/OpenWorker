using System.Diagnostics.Metrics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Hosting;
using OpenWorker.DependencyInjection;
using OpenWorker.DependencyInjection.Health;
using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Telemetry;

namespace OpenWorker.AuthServer.Test.DependencyInjection;

/// <summary>
/// A real host of the world-loop servers: the same registrations <c>Program.cs</c> makes through
/// <c>AddHotspot</c>, the game listener on a free port and the health endpoint on another, the database in memory.
/// Redis is not part of it, so a test needs neither Redis nor Postgres.
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
        var (gamePort, healthPort) = FreePorts();

        var configuration = new Dictionary<string, string?>
        {
            ["Server:Port"] = gamePort.ToString(CultureInfo.InvariantCulture),
            ["Game:Version"] = "1.4.2.3",
            ["Hotspot:PacketDumpPath"] = "",
            ["Hotspot:ErrorBudget:MaxErrors"] = "10",
            ["Hotspot:ErrorBudget:Window"] = "00:01:00",
            ["Health:Port"] = healthPort.ToString(CultureInfo.InvariantCulture),
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

        return new ServerHarness(host, gamePort, healthPort);
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
    /// Two different free ports. Both listeners are open while the ports are read: a port that was just closed is
    /// what the system hands out next, so two calls in a row could return the same port, and then the health
    /// endpoint would be told to share the port of the game listener.
    /// </summary>
    private static (int Game, int Health) FreePorts()
    {
        var game = new TcpListener(IPAddress.Loopback, 0);
        var health = new TcpListener(IPAddress.Loopback, 0);

        game.Start();
        health.Start();

        try
        {
            return (((IPEndPoint)game.LocalEndpoint).Port, ((IPEndPoint)health.LocalEndpoint).Port);
        }

        finally
        {
            game.Stop();
            health.Stop();
        }
    }
}
