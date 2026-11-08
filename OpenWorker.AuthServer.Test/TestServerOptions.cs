using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

    [TestMethod]
    [DataRow(Auth)]
    [DataRow(Gate)]
    [DataRow(District)]
    [DataRow(Maze)]
    [DataRow(Relay)]
    [DataRow(UpdateContent)]
    public void DefaultsOfTheServerPassTheValidation(string server)
    {
        using var host = CreateHost(server);

        Validate(host);
    }

    [TestMethod]
    [DataRow(Auth)]
    [DataRow(Gate)]
    [DataRow(District)]
    [DataRow(Maze)]
    public void ServerNeedsItsListeningPortAndGameVersion(string server)
    {
        var port = Fails(server, new() { ["Server:Port"] = "0" });
        StringAssert.Contains(port.Message, "Server:Port");

        var version = Fails(server, new() { ["Game:Version"] = "" });
        StringAssert.Contains(version.Message, "Game:Version");
    }

    [TestMethod]
    [DataRow(Auth)]
    [DataRow(Gate)]
    [DataRow(District)]
    [DataRow(Maze)]
    public void SessionMayFailTenPacketsInAMinuteByDefault(string server)
    {
        using var host = CreateHost(server);

        Validate(host);

        var budget = host.Services.GetRequiredService<IOptions<HandlerErrorBudgetOptions>>().Value;

        Assert.AreEqual(10, budget.MaxErrors);
        Assert.AreEqual(TimeSpan.FromMinutes(1), budget.Window);
    }

    [TestMethod]
    [DataRow(Auth)]
    [DataRow(Gate)]
    [DataRow(District)]
    [DataRow(Maze)]
    public void ErrorBudgetNeedsAtLeastOneErrorAndAWindowThatHasLength(string server)
    {
        var none = Fails(server, new() { ["Hotspot:ErrorBudget:MaxErrors"] = "0" });
        StringAssert.Contains(none.Message, "Hotspot:ErrorBudget:MaxErrors");

        var negative = Fails(server, new() { ["Hotspot:ErrorBudget:MaxErrors"] = "-5" });
        StringAssert.Contains(negative.Message, "Hotspot:ErrorBudget:MaxErrors");

        var noWindow = Fails(server, new() { ["Hotspot:ErrorBudget:Window"] = "00:00:00" });
        StringAssert.Contains(noWindow.Message, "Hotspot:ErrorBudget:Window");

        var backwards = Fails(server, new() { ["Hotspot:ErrorBudget:Window"] = "-00:01:00" });
        StringAssert.Contains(backwards.Message, "Hotspot:ErrorBudget:Window");
    }

    [TestMethod]
    public void ErrorBudgetCanBeChangedFromTheEnvironment()
    {
        // `Hotspot__ErrorBudget__MaxErrors=3` and `Hotspot__ErrorBudget__Window=00:00:30` in the environment of a container.
        using var host = CreateHost(District, new() { ["Hotspot:ErrorBudget:MaxErrors"] = "3", ["Hotspot:ErrorBudget:Window"] = "00:00:30" });

        Validate(host);

        var budget = host.Services.GetRequiredService<IOptions<HandlerErrorBudgetOptions>>().Value;

        Assert.AreEqual(3, budget.MaxErrors);
        Assert.AreEqual(TimeSpan.FromSeconds(30), budget.Window);
    }

    [TestMethod]
    public void ErrorBudgetNeedsToBeInTheFileOfEveryServerThatHandlesPackets()
    {
        // No default in code: a server whose file lacks the section stops at start and names the key.
        var error = Fails(Maze, new() { ["Hotspot:ErrorBudget:MaxErrors"] = null, ["Hotspot:ErrorBudget:Window"] = null });

        StringAssert.Contains(error.Message, "Hotspot:ErrorBudget:MaxErrors");
        StringAssert.Contains(error.Message, "Hotspot:ErrorBudget:Window");
    }

    [TestMethod]
    [DataRow(Auth)]
    [DataRow(Gate)]
    [DataRow(District)]
    [DataRow(Maze)]
    [DataRow(Relay)]
    public void HealthEndpointListensOnItsOwnPortByDefault(string server)
    {
        using var host = CreateHost(server);

        Validate(host);

        var health = host.Services.GetRequiredService<IOptions<HealthOptions>>().Value;

        Assert.AreEqual(8080, health.Port);
        Assert.AreEqual(TimeSpan.FromSeconds(5), health.LoopTimeout);
        Assert.AreEqual(TimeSpan.FromSeconds(3), health.CheckTimeout);
    }

    [TestMethod]
    [DataRow(Auth)]
    [DataRow(Gate)]
    [DataRow(District)]
    [DataRow(Maze)]
    [DataRow(Relay)]
    public void HealthEndpointNeedsAPortAndTimesThatHaveLength(string server)
    {
        var port = Fails(server, new() { ["Health:Port"] = "0" });
        StringAssert.Contains(port.Message, "Health:Port");

        var missing = Fails(server, new() { ["Health:Port"] = null });
        StringAssert.Contains(missing.Message, "Health:Port");

        var loop = Fails(server, new() { ["Health:LoopTimeout"] = "00:00:00" });
        StringAssert.Contains(loop.Message, "Health:LoopTimeout");

        var check = Fails(server, new() { ["Health:CheckTimeout"] = "-00:00:03" });
        StringAssert.Contains(check.Message, "Health:CheckTimeout");
    }

    [TestMethod]
    [DataRow(Auth)]
    [DataRow(Gate)]
    [DataRow(District)]
    [DataRow(Maze)]
    public void HealthEndpointCannotTakeThePortOfThePlayers(string server)
    {
        // Two listeners on one port: one of them would fail to start, and the failure would not name the setting.
        var error = Fails(server, new() { ["Health:Port"] = "10000", ["Server:Port"] = "10000" });

        StringAssert.Contains(error.Message, "Health:Port");
        StringAssert.Contains(error.Message, "Server:Port");
    }

    [TestMethod]
    [DataRow(Auth)]
    [DataRow(Gate)]
    [DataRow(District)]
    [DataRow(Maze)]
    public void ServerStopsInThirtySecondsOfWhichTheDrainTakesFifteenByDefault(string server)
    {
        using var host = CreateHost(server);

        Validate(host);

        var shutdown = host.Services.GetRequiredService<IOptions<ShutdownOptions>>().Value;

        Assert.AreEqual(TimeSpan.FromSeconds(30), shutdown.Timeout);
        Assert.AreEqual(TimeSpan.FromSeconds(15), shutdown.DrainTimeout);
    }

    [TestMethod]
    [DataRow(Auth)]
    [DataRow(Gate)]
    [DataRow(District)]
    [DataRow(Maze)]
    public void DrainNeedsATimeAndHasToEndBeforeTheHostGivesUp(string server)
    {
        var none = Fails(server, new() { ["Shutdown:DrainTimeout"] = "00:00:00" });
        StringAssert.Contains(none.Message, "Shutdown:DrainTimeout");

        // The services that stop after the drain would get no time at all.
        var same = Fails(server, new() { ["Shutdown:Timeout"] = "00:00:15" });
        StringAssert.Contains(same.Message, "Shutdown:Timeout");

        var backwards = Fails(server, new() { ["Shutdown:Timeout"] = "00:00:10", ["Shutdown:DrainTimeout"] = "00:00:20" });
        StringAssert.Contains(backwards.Message, "Shutdown:Timeout");
    }

    [TestMethod]
    [DataRow(Auth)]
    [DataRow(Gate)]
    [DataRow(District)]
    [DataRow(Maze)]
    [DataRow(Relay)]
    public void MetricsGoToTheCollectorOfComposeByDefault(string server)
    {
        using var host = CreateHost(server);

        Validate(host);

        var telemetry = host.Services.GetRequiredService<IOptions<TelemetryOptions>>().Value;

        Assert.IsTrue(telemetry.IsExporting);
        Assert.AreEqual("http://prometheus:9090/api/v1/otlp/v1/metrics", telemetry.Endpoint);
        Assert.AreEqual(TelemetryProtocol.HttpProtobuf, telemetry.Protocol);
        Assert.AreEqual(TimeSpan.FromSeconds(10), telemetry.ExportInterval);
    }

    [TestMethod]
    [DataRow(Auth)]
    [DataRow(Relay)]
    public void AnEmptyEndpointTurnsTheExportOff(string server)
    {
        // `Telemetry__Endpoint=` in the environment of a container is an empty value over the default of the file.
        using var host = CreateHost(server, new() { ["Telemetry:Endpoint"] = "" });

        Validate(host);

        Assert.IsFalse(host.Services.GetRequiredService<IOptions<TelemetryOptions>>().Value.IsExporting);
    }

    [TestMethod]
    [DataRow(Auth, "prometheus:9090")]
    [DataRow(District, "ftp://prometheus/metrics")]
    [DataRow(Maze, "not an address")]
    [DataRow(Relay, "/api/v1/otlp")]
    public void EndpointOfTheCollectorMustBeAnHttpAddress(string server, string endpoint)
    {
        var error = Fails(server, new() { ["Telemetry:Endpoint"] = endpoint });

        StringAssert.Contains(error.Message, "Telemetry:Endpoint");
    }

    [TestMethod]
    [DataRow(Auth, "00:00:00")]
    [DataRow(Gate, "00:00:00.5")]
    [DataRow(District, "-00:00:10")]
    [DataRow(Relay, null)]
    public void MetricsAreSentAtLeastOnceASecond(string server, string? interval)
    {
        var error = Fails(server, new() { ["Telemetry:ExportInterval"] = interval });

        StringAssert.Contains(error.Message, "Telemetry:ExportInterval");
    }

    [TestMethod]
    public void ProtocolOfTheExportIsOneTheExporterKnows()
    {
        using var grpc = CreateHost(Auth, new() { ["Telemetry:Protocol"] = "Grpc" });

        Validate(grpc);

        Assert.AreEqual(TelemetryProtocol.Grpc, grpc.Services.GetRequiredService<IOptions<TelemetryOptions>>().Value.Protocol);

        var error = Fails(Auth, new() { ["Telemetry:Protocol"] = "7" });

        StringAssert.Contains(error.Message, "Telemetry:Protocol");
    }

    [TestMethod]
    public void GameVersionIsNeverAPath()
    {
        // The version names the folder of the packet dumps.
        var error = Fails(Auth, new() { ["Game:Version"] = "../../etc" });

        StringAssert.Contains(error.Message, "Game:Version");
    }

    [TestMethod]
    [DataRow(Auth)]
    [DataRow(Gate)]
    [DataRow(District)]
    [DataRow(Maze)]
    [DataRow(Relay)]
    public void RedisAndPostgresAreRequired(string server)
    {
        var redis = Fails(server, new() { ["ConnectionStrings:Redis"] = "" });
        StringAssert.Contains(redis.Message, "ConnectionStrings:Redis");

        var postgres = Fails(server, new() { ["ConnectionStrings:Postgres"] = "" });
        StringAssert.Contains(postgres.Message, "ConnectionStrings:Postgres");
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

    [TestMethod]
    public void UpdateContentNeedsOnlyTheDatabase()
    {
        var error = Fails(UpdateContent, new() { ["ConnectionStrings:Postgres"] = "" });

        StringAssert.Contains(error.Message, "ConnectionStrings:Postgres");
    }

    [TestMethod]
    [DataRow("localhost:6379")]
    [DataRow("http://redis:6379")]
    [DataRow("redis://")]
    public void RedisMustBeAnAddress(string address)
    {
        var error = Fails(Auth, new() { ["ConnectionStrings:Redis"] = address });

        StringAssert.Contains(error.Message, "ConnectionStrings:Redis");
    }

    [TestMethod]
    public void DistrictNeedsTheLocationItServes()
    {
        var error = Fails(District, new() { ["Instance:Location"] = "0" });

        StringAssert.Contains(error.Message, "Instance:Location");
    }

    [TestMethod]
    public void DistrictNeedsItsOwnPublicPort()
    {
        // Every district listens behind its own port, so there is no default to fall back to.
        var error = Fails(District, new() { ["Instance:PublicPort"] = null });

        StringAssert.Contains(error.Message, "Instance:PublicPort");
    }

    [TestMethod]
    [DataRow(District)]
    [DataRow(Maze)]
    public void DistrictAndMazeAssignTheirIdentifier(string server)
    {
        var error = Fails(server, new() { [ConfigurationUtils.InstanceIdentifierKey] = null });

        StringAssert.Contains(error.Message, "Instance:Identifier");
    }

    [TestMethod]
    [DataRow(Gate)]
    [DataRow(District)]
    [DataRow(Maze)]
    public void EveryServerOfAGateNeedsItsNumber(string server)
    {
        var error = Fails(server, new() { ["Gate:Id"] = "0" });

        StringAssert.Contains(error.Message, "Gate:Id");
    }

    [TestMethod]
    public void GateServerNeedsTheAddressItIsReachedAt()
    {
        var host = Fails(Gate, new() { ["Gate:Host"] = "" });
        StringAssert.Contains(host.Message, "Gate:Host");

        var port = Fails(Gate, new() { ["Gate:Port"] = "0" });
        StringAssert.Contains(port.Message, "Gate:Port");
    }

    [TestMethod]
    [DataRow(Gate)]
    [DataRow(District)]
    [DataRow(Maze)]
    public void ServersThatSendPlayersAwayNeedTheAddressOfAuthAndMaze(string server)
    {
        var auth = Fails(server, new() { ["Auth:Host"] = "" });
        StringAssert.Contains(auth.Message, "Auth:Host");

        var maze = Fails(server, new() { ["Maze:Port"] = "70000" });
        StringAssert.Contains(maze.Message, "Maze:Port");
    }

    [TestMethod]
    public void ChannelsRunFromTheFirstToTheLast()
    {
        var error = Fails(District, new() { ["Channel:From"] = "5", ["Channel:To"] = "1" });

        StringAssert.Contains(error.Message, "Channel:To");
    }

    [TestMethod]
    public void PublicPortOfAMazeIsOptional()
    {
        using var without = CreateHost(Maze);
        using var with = CreateHost(Maze, new() { ["Instance:PublicPort"] = "10102" });

        Validate(without);
        Validate(with);

        Assert.IsNull(without.Services.GetRequiredService<IOptions<InstanceOptions>>().Value.PublicPort);
        Assert.AreEqual(10102, with.Services.GetRequiredService<IOptions<InstanceOptions>>().Value.PublicPort);
    }

    [TestMethod]
    public void EmptyPacketDumpPathTurnsTheDumpsOff()
    {
        // `Hotspot__PacketDumpPath=` in the environment of a container is an empty value over the default of the file.
        using var host = CreateHost(Auth, new() { ["Hotspot:PacketDumpPath"] = "" });

        Validate(host);

        Assert.IsTrue(string.IsNullOrWhiteSpace(host.Services.GetRequiredService<IOptions<HotspotOptions>>().Value.PacketDumpPath));
    }

    [TestMethod]
    public void PacketDumpsAreOnByDefault()
    {
        using var host = CreateHost(Auth);

        Assert.AreEqual("test/hotspot", host.Services.GetRequiredService<IOptions<HotspotOptions>>().Value.PacketDumpPath);
    }

    [TestMethod]
    public void RelayNeedsTheDiscordToken()
    {
        // The token is a secret, so no file carries it and the relay stops at start when the environment has none.
        var missing = Fails(Relay, new() { ["Credentials:DiscordToken"] = null });
        StringAssert.Contains(missing.Message, "Credentials:DiscordToken");

        var empty = Fails(Relay, new() { ["Credentials:DiscordToken"] = "" });
        StringAssert.Contains(empty.Message, "Credentials:DiscordToken");
    }

    [TestMethod]
    [DataRow(District, "Attendance:ResetHour")]
    [DataRow(District, "DailyMission:ResetHour")]
    [DataRow(Maze, "Attendance:ResetHour")]
    [DataRow(Maze, "DailyMission:ResetHour")]
    public void ResetHourIsAnHourOfTheDay(string server, string key)
    {
        var tooLate = Fails(server, new() { [key] = "24" });
        StringAssert.Contains(tooLate.Message, key);

        var negative = Fails(server, new() { [key] = "-1" });
        StringAssert.Contains(negative.Message, key);

        using var halfPast = CreateHost(server, new() { [key] = "9.5" });

        Validate(halfPast);
    }

    [TestMethod]
    public void StartLevelIsALevelOfTheGame()
    {
        var zero = Fails(Gate, new() { ["PersonCreation:StartLevel"] = "0" });
        StringAssert.Contains(zero.Message, "PersonCreation:StartLevel");

        var tooHigh = Fails(Gate, new() { ["PersonCreation:StartLevel"] = "101" });
        StringAssert.Contains(tooHigh.Message, "PersonCreation:StartLevel");

        using var host = CreateHost(Gate);

        Validate(host);

        // A new character starts at the first level unless the file says otherwise.
        Assert.AreEqual((byte)1, host.Services.GetRequiredService<IOptions<PersonCreationOptions>>().Value.StartLevel);
    }

    [TestMethod]
    [DataRow(District, "Rewards:ExpRate")]
    [DataRow(District, "Rewards:DropRate")]
    [DataRow(Maze, "Rewards:ExpRate")]
    [DataRow(Maze, "Rewards:DropRate")]
    public void RewardRatesAreNotNegative(string server, string key)
    {
        var negative = Fails(server, new() { [key] = "-1" });
        StringAssert.Contains(negative.Message, key);

        using var doubled = CreateHost(server, new() { [key] = "2.5" });

        Validate(doubled);
    }

    [TestMethod]
    public void ResetHoursAreOptional()
    {
        // Without them the rules of the features decide.
        using var host = CreateHost(District);

        Validate(host);

        Assert.IsNull(host.Services.GetRequiredService<IOptions<AttendanceOptions>>().Value.ResetHour);
        Assert.IsNull(host.Services.GetRequiredService<IOptions<DailyMissionOptions>>().Value.ResetHour);
    }

    [TestMethod]
    public void MazeAddressOfAPlayerTransferComesFromTheFile()
    {
        // WorldManager sends players here while no maze server has registered itself.
        using var host = CreateHost(District);

        Validate(host);

        var maze = host.Services.GetRequiredService<IOptions<MazeEndpointOptions>>().Value;

        Assert.AreEqual("127.0.0.1", maze.Host);
        Assert.AreEqual(10101, maze.Port);
    }

    /// <summary>
    /// The classes of a server take these as <see cref="IOptions{TOptions}"/>: one that the server does not register
    /// would stop the class from being built when the first player connects, not when the host starts.
    /// </summary>
    [TestMethod]
    [DataRow(Gate, typeof(GateOptions), typeof(GateEndpointOptions), typeof(MazeEndpointOptions), typeof(AuthEndpointOptions))]
    [DataRow(District, typeof(InstanceOptions), typeof(GateOptions), typeof(MazeEndpointOptions), typeof(ChannelOptions))]
    [DataRow(Maze, typeof(InstanceOptions), typeof(GateOptions), typeof(MazeEndpointOptions), typeof(ChannelOptions))]
    public void ServerRegistersTheOptionsItsClassesRead(string server, params Type[] options)
    {
        using var host = CreateHost(server);

        Validate(host);

        foreach (var type in options)
        {
            var service = host.Services.GetRequiredService(typeof(IOptions<>).MakeGenericType(type));
            var value = service.GetType().GetProperty(nameof(IOptions<object>.Value))!.GetValue(service);

            Assert.IsNotNull(value, $"{server} has no {type.Name}.");
        }
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

    [TestMethod]
    public void EveryServerShipsItsDefaults()
    {
        foreach (var server in AllServers)
        {
            Assert.IsTrue(File.Exists(AppSettings(server)), $"{server} has no appsettings.json next to the tests.");
        }
    }

    public TestContext TestContext { get; set; } = null!;
}
