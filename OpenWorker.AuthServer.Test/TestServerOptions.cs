using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using OpenWorker.Channel.Extensions;
using OpenWorker.DependencyInjection;
using OpenWorker.Extensions;

namespace OpenWorker.AuthServer.Test;

/// <summary>
/// The settings of the servers are checked when the host starts: the defaults of every server pass, and a missing
/// or wrong setting stops the host with a message that names it. The tests read the <c>appsettings.json</c> each
/// server project ships, and register what the modules of that server register.
/// </summary>
[TestClass]
public sealed class TestServerOptions
{
    private const string Auth = "AuthServer";
    private const string Gate = "GateServer";
    private const string District = "DistrictServer";
    private const string Maze = "MazeServer";
    private const string Relay = "RelayServer";
    private const string UpdateContent = "UpdateContent";

    private static readonly string[] AllServers = [Auth, Gate, District, Maze, Relay, UpdateContent];

    /// <summary>
    /// What a server does not carry in <c>appsettings.json</c>: the district's own address, which differs per
    /// district, and the identifier the district and maze servers assign themselves.
    /// </summary>
    private static Dictionary<string, string?> Runtime(string server) => server switch
    {
        District => new()
        {
            [ConfigurationUtils.InstanceIdentifierKey] = Guid.NewGuid().ToString(),
            ["Instance:Location"] = "10003",
            ["Instance:PublicPort"] = "10011"
        },
        Maze => new() { [ConfigurationUtils.InstanceIdentifierKey] = Guid.NewGuid().ToString() },
        Relay => new() { ["Credentials:DiscordToken"] = "test-token" },
        _ => []
    };

    /// <summary>
    /// What the modules of each server register, the way its <c>Program.cs</c> does.
    /// </summary>
    private static void Register(string server, IServiceCollection services)
    {
        switch (server)
        {
            case Auth:
                services.AddServerOptions();
                services.AddCache();
                services.AddPersistence();
                OpenWorker.AuthServer.App.Extensions.ServiceCollectionExtensions.AddGameplay(services);
                break;

            case Gate:
                services.AddServerOptions();
                services.AddCache();
                services.AddPersistence();
                OpenWorker.GateServer.Gameplay.Extensions.DependencyInjection.AddGameplay(services);
                services.AddPlayerTransfer();
                services.AddGateOptions();
                services.AddGateEndpointOptions();
                services.AddPersonCreationOptions();
                break;

            case District:
                services.AddServerOptions();
                services.AddCache();
                services.AddPersistence();
                services.AddDistrictOptions();
                services.AddPlayerTransfer();
                services.AddChannels();
                services.AddAttendanceOptions();
                services.AddRewardsOptions();
                services.AddCommandOptions();
                services.AddDailyMissionOptions();
                break;

            case Maze:
                services.AddServerOptions();
                services.AddCache();
                services.AddPersistence();
                services.AddGateOptions();
                services.AddPlayerTransfer();
                services.AddChannels();
                services.AddAttendanceOptions();
                services.AddRewardsOptions();
                services.AddCommandOptions();
                services.AddDailyMissionOptions();
                break;

            case Relay:
                services.AddCache();
                services.AddPersistence();
                services.AddTelemetryOptions();
                services.AddCredentialsOptions();
                break;

            case UpdateContent:
                services.AddPersistenceOptions();
                break;

            default:
                throw new ArgumentException($"Unknown server {server}.", nameof(server));
        }
    }

    private static string AppSettings(string server)
    {
        return Path.Combine(AppContext.BaseDirectory, "Settings", $"{server}.json");
    }

    private static IHost CreateHost(string server, Dictionary<string, string?>? changes = null)
    {
        return new HostBuilder()
            .ConfigureAppConfiguration(configuration => configuration
                .AddJsonFile(AppSettings(server), optional: false)
                .AddInMemoryCollection(Runtime(server))
                .AddInMemoryCollection(changes ?? []))
            .ConfigureServices((_, services) => Register(server, services))
            .Build();
    }

    /// <summary>
    /// What the host does before it starts any service. The hosted services of the modules are not started here:
    /// they need Redis and the database.
    /// </summary>
    private static void Validate(IHost host)
    {
        host.Services.GetRequiredService<IStartupValidator>().Validate();
    }

    private static OptionsValidationException Fails(string server, Dictionary<string, string?> changes)
    {
        using var host = CreateHost(server, changes);

        return Assert.ThrowsExactly<OptionsValidationException>(() => Validate(host));
    }

    /// <summary>
    /// The options the classes of each server take as <see cref="IOptions{TOptions}"/>: one that the server does not
    /// register would stop the class from being built when the first player connects, not when the host starts.
    /// </summary>
    private static readonly Dictionary<string, Type[]> OptionsReadByClasses = new()
    {
        [Gate] = [typeof(GateOptions), typeof(GateEndpointOptions), typeof(MazeEndpointOptions), typeof(AuthEndpointOptions)],
        [District] = [typeof(InstanceOptions), typeof(GateOptions), typeof(MazeEndpointOptions), typeof(ChannelOptions)],
        [Maze] = [typeof(InstanceOptions), typeof(GateOptions), typeof(MazeEndpointOptions), typeof(ChannelOptions)]
    };

    /// <summary>
    /// Every server ships an <c>appsettings.json</c> whose settings pass the checks of the host, and registers the
    /// options its classes read.
    /// </summary>
    [TestMethod]
    public void EveryServerStartsWithTheSettingsItShips()
    {
        foreach (var server in AllServers)
        {
            Assert.IsTrue(File.Exists(AppSettings(server)), $"{server} has no appsettings.json next to the tests.");

            using var host = CreateHost(server);

            Validate(host);

            foreach (var type in OptionsReadByClasses.GetValueOrDefault(server, []))
            {
                var service = host.Services.GetRequiredService(typeof(IOptions<>).MakeGenericType(type));
                var value = service.GetType().GetProperty(nameof(IOptions<object>.Value))!.GetValue(service);

                Assert.IsNotNull(value, $"{server} has no {type.Name}.");
            }
        }
    }

    /// <summary>
    /// A setting the server cannot run without stops the host at start, and the message names it. Each case reaches
    /// another check: a section with no default in code, a secret no file carries, what only a district must have, and
    /// what the server assigns itself before the options are read.
    /// </summary>
    [TestMethod]
    public void AMissingSettingStopsTheHostAndNamesIt()
    {
        (string Server, Dictionary<string, string?> Changes, string[] Keys)[] cases =
        [
            (Maze, new() { ["Hotspot:ErrorBudget:MaxErrors"] = null, ["Hotspot:ErrorBudget:Window"] = null },
                ["Hotspot:ErrorBudget:MaxErrors", "Hotspot:ErrorBudget:Window"]),
            (Relay, new() { ["Credentials:DiscordToken"] = null }, ["Credentials:DiscordToken"]),
            (District, new() { ["Instance:PublicPort"] = null }, ["Instance:PublicPort"]),
            (Maze, new() { [ConfigurationUtils.InstanceIdentifierKey] = null }, ["Instance:Identifier"])
        ];

        foreach (var (server, changes, keys) in cases)
        {
            var error = Fails(server, changes);

            foreach (var key in keys)
            {
                StringAssert.Contains(error.Message, key, $"{server} without {string.Join(", ", changes.Keys)}");
            }
        }
    }

    /// <summary>
    /// A setting with a value the server cannot use stops the host at start, and the message names it. One case for
    /// each rule the settings are checked by, the rules that weigh two settings together included.
    /// </summary>
    [TestMethod]
    public void AWrongSettingStopsTheHostAndNamesIt()
    {
        (string Server, Dictionary<string, string?> Changes, string[] Keys)[] cases =
        [
            (Auth, new() { ["Server:Port"] = "0" }, ["Server:Port"]),
            // The version names the folder of the packet dumps.
            (Auth, new() { ["Game:Version"] = "../../etc" }, ["Game:Version"]),
            (Auth, new() { ["ConnectionStrings:Redis"] = "http://redis:6379" }, ["ConnectionStrings:Redis"]),
            (Gate, new() { ["Hotspot:ErrorBudget:Window"] = "00:00:00" }, ["Hotspot:ErrorBudget:Window"]),
            (Relay, new() { ["Health:LoopTimeout"] = "00:00:00" }, ["Health:LoopTimeout"]),
            // Two listeners on one port: one of them would fail to start, and the failure would not name the setting.
            (Gate, new() { ["Health:Port"] = "10000", ["Server:Port"] = "10000" }, ["Health:Port", "Server:Port"]),
            // The services that stop after the drain would get no time at all.
            (District, new() { ["Shutdown:Timeout"] = "00:00:15", ["Shutdown:DrainTimeout"] = "00:00:15" }, ["Shutdown:Timeout"]),
            (Maze, new() { ["Telemetry:Endpoint"] = "ftp://prometheus/metrics" }, ["Telemetry:Endpoint"]),
            (Auth, new() { ["Telemetry:Protocol"] = "7" }, ["Telemetry:Protocol"]),
            (Gate, new() { ["Telemetry:ExportInterval"] = "00:00:00.5" }, ["Telemetry:ExportInterval"]),
            (District, new() { ["Instance:Location"] = "0" }, ["Instance:Location"]),
            (District, new() { ["Channel:From"] = "5", ["Channel:To"] = "1" }, ["Channel:To"]),
            (District, new() { ["Attendance:ResetHour"] = "24" }, ["Attendance:ResetHour"]),
            (Maze, new() { ["DailyMission:ResetHour"] = "-1" }, ["DailyMission:ResetHour"]),
            (Maze, new() { ["Rewards:DropRate"] = "-1" }, ["Rewards:DropRate"])
        ];

        foreach (var (server, changes, keys) in cases)
        {
            var error = Fails(server, changes);

            foreach (var key in keys)
            {
                StringAssert.Contains(error.Message, key, $"{server} with {string.Join(", ", changes)}");
            }
        }
    }

    /// <summary>
    /// What may be left empty turns its feature off instead of stopping the host: an empty endpoint of the collector
    /// (<c>Telemetry__Endpoint=</c> in the environment of a container) stops the export, a maze needs no public port,
    /// and without reset hours the rules of the features decide.
    /// </summary>
    [TestMethod]
    public void ASettingThatMayBeEmptyTurnsItsFeatureOff()
    {
        using var auth = CreateHost(Auth, new() { ["Telemetry:Endpoint"] = "" });
        using var maze = CreateHost(Maze);
        using var district = CreateHost(District);

        Validate(auth);
        Validate(maze);
        Validate(district);

        Assert.IsFalse(auth.Services.GetRequiredService<IOptions<TelemetryOptions>>().Value.IsExporting);
        Assert.IsNull(maze.Services.GetRequiredService<IOptions<InstanceOptions>>().Value.PublicPort);
        Assert.IsNull(district.Services.GetRequiredService<IOptions<AttendanceOptions>>().Value.ResetHour);
        Assert.IsNull(district.Services.GetRequiredService<IOptions<DailyMissionOptions>>().Value.ResetHour);
    }

    [TestMethod]
    public async Task ValidationRunsBeforeAnyServiceStarts()
    {
        // The migration is the first service to start. A setting that is wrong must stop the host before any service
        // starts, the first one included, even when no service reads that setting.
        var started = new Started();

        using var host = new HostBuilder()
            .ConfigureAppConfiguration(configuration => configuration
                .AddJsonFile(AppSettings(Auth), optional: false)
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Server:Port"] = "0" }))
            .ConfigureServices((_, services) =>
            {
                Register(Auth, services);
                services.Insert(0, ServiceDescriptor.Singleton<IHostedService>(started));
            })
            .Build();

        var error = await Assert.ThrowsExactlyAsync<OptionsValidationException>(() => host.StartAsync(TestContext.CancellationToken));

        StringAssert.Contains(error.Message, "Server:Port");
        Assert.IsFalse(started.Value, "A service started before the settings were checked.");
    }

    private sealed class Started : IHostedService
    {
        public bool Value { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Value = true;

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private static void AssertEndpoint(string host, int port, StackExchange.Redis.ConfigurationOptions options)
    {
        var endpoint = Assert.IsInstanceOfType<System.Net.DnsEndPoint>(options.EndPoints.Single());

        Assert.AreEqual(host, endpoint.Host);
        Assert.AreEqual(port, endpoint.Port);
    }

    [TestMethod]
    public void OneRedisAddressServesTheCacheAndTheNotifications()
    {
        var options = new RedisOptions { ConnectionString = "redis://redis:6379" }.ToConfigurationOptions();

        AssertEndpoint("redis", 6379, options);
        Assert.IsFalse(options.Ssl);
        Assert.IsFalse(options.AbortOnConnectFail, "A server that starts before Redis is ready keeps trying.");
        Assert.IsNull(options.Password);
    }

    [TestMethod]
    public void RedisAddressWithALoginAndTls()
    {
        var options = new RedisOptions { ConnectionString = "rediss://worker:p%40ss@cache.example:6380" }.ToConfigurationOptions();

        AssertEndpoint("cache.example", 6380, options);
        Assert.IsTrue(options.Ssl);
        Assert.AreEqual("worker", options.User);
        Assert.AreEqual("p@ss", options.Password);
    }

    [TestMethod]
    public void RedisAddressWithoutAPortUsesTheStandardOne()
    {
        var options = new RedisOptions { ConnectionString = "redis://redis" }.ToConfigurationOptions();

        AssertEndpoint("redis", 6379, options);
    }

    /// <summary>
    /// The console prints the scope of a session (<c>SessionLogScope</c>) after each line. The options of the formatter
    /// are the place for it: the older <c>Logging:Console:IncludeScopes</c> replaces them with defaults.
    /// </summary>
    [TestMethod]
    public void TheConsolePrintsTheScopes()
    {
        foreach (var server in AllServers.Where(server => server != UpdateContent))
        {
            using var host = new HostBuilder()
                .ConfigureAppConfiguration(configuration => configuration.AddJsonFile(AppSettings(server), optional: false))
                .ConfigureLogging((context, logging) => logging
                    .AddConfiguration(context.Configuration.GetSection("Logging"))
                    .AddConsole())
                .Build();

            var console = host.Services.GetRequiredService<IOptionsMonitor<ConsoleLoggerOptions>>().CurrentValue;
            var simple = host.Services.GetRequiredService<IOptionsMonitor<SimpleConsoleFormatterOptions>>().CurrentValue;

            Assert.AreEqual(ConsoleFormatterNames.Simple, console.FormatterName, server);
            Assert.IsTrue(simple.IncludeScopes, $"{server} does not print the scopes of its log lines.");
        }
    }

    public TestContext TestContext { get; set; } = null!;
}
