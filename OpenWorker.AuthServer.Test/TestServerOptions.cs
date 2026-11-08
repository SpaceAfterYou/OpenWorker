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
                services.AddGateEndpointOptions();
                break;

            case District:
                services.AddServerOptions();
                services.AddCache();
                services.AddPersistence();
                services.AddDistrictOptions();
                services.AddPlayerTransfer();
                services.AddChannels();
                break;

            case Maze:
                services.AddServerOptions();
                services.AddCache();
                services.AddPersistence();
                services.AddPlayerTransfer();
                services.AddChannels();
                break;

            case Relay:
                services.AddCache();
                services.AddPersistence();
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
    public void MazeAddressForTheClassesThatReadTheConfigurationStillComesFromTheFile()
    {
        // WorldManager reads it through the configuration until it moves to the options.
        var configuration = new ConfigurationBuilder().AddJsonFile(AppSettings(District), optional: false).Build();

        Assert.AreEqual(("127.0.0.1", (short)10101), configuration.GetMazeAddress());

        var empty = new ConfigurationBuilder().Build();

        Assert.ThrowsExactly<InvalidOperationException>(() => empty.GetMazeAddress());
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
