using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Channel.Extensions;
using OpenWorker.DependencyInjection;
using OpenWorker.Hotspot;
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

    public TestContext TestContext { get; set; } = null!;
}
