using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Channel.Extensions;
using OpenWorker.DependencyInjection;
using OpenWorker.Hotspot;
using OpenWorker.Extensions;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Cache.Types;
using Redis.OM;
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

    public TestContext TestContext { get; set; } = null!;
}
