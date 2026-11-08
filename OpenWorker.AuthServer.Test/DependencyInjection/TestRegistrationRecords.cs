using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Channel.Extensions;
using OpenWorker.DependencyInjection;
using OpenWorker.Hotspot;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Extensions;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Cache.Types;
using Redis.OM;
using Redis.OM.Modeling;
using Redis.OM.Searching;

namespace OpenWorker.AuthServer.Test.DependencyInjection;

/// <summary>
/// What a server leaves in Redis, and takes back when it drains. Needs Redis with the search module: set
/// OPENWORKER_TEST_REDIS, for example redis://127.0.0.1:6379.
/// </summary>
[TestClass]
public sealed class TestRegistrationRecords
{
    private static string Redis { get; set; } = string.Empty;

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext _)
    {
        Redis = Environment.GetEnvironmentVariable("OPENWORKER_TEST_REDIS") ?? string.Empty;

        if (string.IsNullOrEmpty(Redis))
        {
            Assert.Inconclusive("OPENWORKER_TEST_REDIS is not set.");
        }

        await new RedisConnectionProvider(Redis).Connection.CreateIndexAsync(typeof(ChannelCache));
    }

    private static ChannelCache Channel(Guid owner, short identifier) => new()
    {
        Owner = owner,
        Gate = 1,
        Location = 10003,
        Identifier = identifier,
        OnlineCount = 0
    };

    [TestMethod]
    public async Task ChannelsOfTheServerAreRemovedAndTheOnesOfOtherServersStay()
    {
        var instance = Guid.NewGuid();
        var other = Guid.NewGuid();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Redis"] = Redis,
                ["Instance:Identifier"] = instance.ToString(),
                ["Instance:PublicHost"] = "127.0.0.1",
                ["Instance:Location"] = "10003",
                ["Gate:Id"] = "1",
                ["Channel:From"] = "1",
                ["Channel:To"] = "3"
            })
            .Build();

        await using var provider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddLogging()
            .AddSingleton(World.Create())
            .AddCache()
            .AddChannelCache()
            .AddChannels()
            .BuildServiceProvider();

        var channels = provider.GetRequiredService<IRedisCollection<ChannelCache>>();

        await channels.InsertAsync(Channel(instance, 1));
        await channels.InsertAsync(Channel(instance, 2));
        await channels.InsertAsync(Channel(instance, 3));
        await channels.InsertAsync(Channel(other, 1));

        // The only registration of this host is the service that keeps the count of the channels.
        var registration = provider.GetServices<IServerRegistration>().Single();

        await registration.WithdrawAsync(TestContext.CancellationToken);

        Assert.IsEmpty(await channels.Where(e => e.Owner == instance).ToArrayAsync(TestContext.CancellationToken));
        Assert.HasCount(1, await channels.Where(e => e.Owner == other).ToArrayAsync(TestContext.CancellationToken));

        await channels.DeleteAsync(await channels.Where(e => e.Owner == other).ToArrayAsync(TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task TheLeastBusyMazeServerIsPickedAndOneWhoseRecordExpiredIsDropped()
    {
        var gate = (short)Random.Shared.Next(1_000, short.MaxValue);

        await using var provider = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Redis"] = Redis })
                .Build())
            .AddLogging()
            .AddCache()
            .BuildServiceProvider();

        var records = provider.GetRequiredService<IRedisCollection<MazeCache>>();
        var directory = provider.GetRequiredService<MazeDirectory>();

        MazeCache Maze(short port, int players) => new()
        {
            Guid = Guid.NewGuid(),
            Gate = gate,
            Players = players,
            Host = "10.0.0." + port.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Port = port
        };

        var busy = Maze(10_201, 9);
        var idle = Maze(10_202, 1);
        var crashed = Maze(10_203, 0);

        foreach (var maze in new[] { busy, idle })
        {
            await records.InsertAsync(maze, TimeSpan.FromSeconds(30));
            await directory.RecordAsync(gate, maze.Guid, maze.Players);
        }

        // A server that crashed: its record has expired, its place in the set is still there and is the emptiest.
        await directory.RecordAsync(gate, crashed.Guid, crashed.Players);

        Assert.AreEqual((idle.Host, idle.Port), await directory.PickAsync(gate));

        var database = provider.GetRequiredService<StackExchange.Redis.IConnectionMultiplexer>().GetDatabase();

        Assert.IsNull(await database.SortedSetScoreAsync(MazeDirectory.Key(gate), crashed.Guid.ToString()), "the pick drops the server that is gone");

        await directory.RemoveAsync(gate, idle.Guid);

        Assert.AreEqual((busy.Host, busy.Port), await directory.PickAsync(gate));

        await directory.RemoveAsync(gate, busy.Guid);
        await records.DeleteAsync([busy, idle]);

        Assert.IsNull(await directory.PickAsync(gate));
    }

    /// <summary>
    /// A document of the test only, so that making its index again cannot disturb the tests that search the real ones.
    /// </summary>
    [Document(StorageType = StorageType.Hash, IndexName = "ow-test-bootstrap-idx", Prefixes = ["ow-test:bootstrap"])]
    public sealed class BootstrapProbe
    {
        [RedisIdField]
        [Indexed]
        public string Id { get; set; } = string.Empty;

        [Indexed]
        public int Group { get; set; }
    }

    [TestMethod]
    public async Task TheBootstrapOfTheRelayKeepsTheRecordsWhenItRunsAgainAndWhenTheSchemaChanges()
    {
        await using var redis = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(
            new RedisOptions { ConnectionString = Redis }.ToConfigurationOptions());

        var provider = new RedisConnectionProvider(redis);
        var probes = Cache.CreateCollection<BootstrapProbe>(provider);
        var database = redis.GetDatabase();
        var type = typeof(BootstrapProbe);

        await provider.Connection.DropIndexAsync(type);
        await database.HashDeleteAsync(RedisIndexes.SchemaKey, type.FullName);

        await RedisIndexes.EnsureAsync(provider, redis, [type], NullLogger.Instance);

        var group = Random.Shared.Next();
        var probe = new BootstrapProbe { Id = Guid.NewGuid().ToString("N"), Group = group };

        await probes.InsertAsync(probe, TimeSpan.FromMinutes(1));

        // The relay starts again while the servers run: nothing changes.
        await RedisIndexes.EnsureAsync(provider, redis, [type], NullLogger.Instance);
        await RedisIndexes.EnsureAsync(provider, redis, [type], NullLogger.Instance);

        Assert.IsNotNull(await probes.FindByIdAsync(probe.Id));
        Assert.HasCount(1, await probes.Where(e => e.Group == group).ToListAsync());

        // A build with another schema: the index is made again, over the record that is there.
        await database.HashSetAsync(RedisIndexes.SchemaKey, type.FullName, "an older schema");

        await RedisIndexes.EnsureAsync(provider, redis, [type], NullLogger.Instance);

        Assert.AreEqual(RedisIndexes.Signature(type), (string?)await database.HashGetAsync(RedisIndexes.SchemaKey, type.FullName));
        Assert.IsNotNull(await probes.FindByIdAsync(probe.Id), "no record is deleted");

        var found = 0;

        // Redis indexes the records that are there in the background, which takes a moment.
        for (var attempt = 0; attempt < 50 && found == 0; attempt++)
        {
            found = (await probes.Where(e => e.Group == group).ToListAsync()).Count;

            if (found == 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), TestContext.CancellationToken);
            }
        }

        Assert.AreEqual(1, found, "the record is found through the index made again");

        await probes.DeleteAsync(probe);
        await provider.Connection.DropIndexAsync(type);
        await database.HashDeleteAsync(RedisIndexes.SchemaKey, type.FullName);
    }

    public TestContext TestContext { get; set; } = null!;
}
