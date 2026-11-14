using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenWorker.Commands;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Stores;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Modules.Attendance;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Gameplay.Modules.Rewards;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Server.Common;
using StackExchange.Redis;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The options of the game: the contents the servers offer and what the client is told of them. Needs Redis: set
/// OPENWORKER_TEST_REDIS.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class GameOptionsTest
{
    private sealed class MemoryContentStore : IServerContentStore
    {
        public ConcurrentDictionary<ServerContentIndex, bool> Rows { get; } = [];

        public Task SetAsync(ServerContentIndex index, bool state, CancellationToken cancellationToken = default)
        {
            Rows[index] = state;

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// One server with its own contents and its own connection to Redis, as a process has.
    /// </summary>
    private sealed class Host : IDisposable
    {
        private readonly ConnectionMultiplexer _redis;

        public Host(MemoryCommunity community, string redis)
        {
            Server = new TestServer(community);
            _redis = ConnectionMultiplexer.Connect(new RedisOptions { ConnectionString = redis }.ToConfigurationOptions());

            var attendance = new AttendanceManager(
                Server.World, new PersonStateStore(null!), AttendanceCatalog.From([], new Dictionary<int, Reward>(), []), null!, Contents,
                TimeProvider.System, Options.Create(new AttendanceOptions()), NullLogger<AttendanceManager>.Instance);

            var notifier = new ServerContentNotifier(
                Server.World, Contents, Server.Network, CashShopTest.EmptyWindow(Server.World, Contents), attendance);

            Changes = new ServerContentChanges(Contents, _redis, Server.Loop, [notifier], NullLogger<ServerContentChanges>.Instance);
            Changes.ListenAsync().GetAwaiter().GetResult();
        }

        public TestServer Server { get; }

        public ServerContent Contents { get; } = new();

        public ServerContentChanges Changes { get; }

        public void Dispose()
        {
            Server.Dispose();
            _redis.Dispose();
        }
    }

    /// <summary>
    /// A content switched by the command on one server is written to the table and switched on every server, and everybody
    /// on each of them is told with 01/07: the client in the game reads the contents only from that message.
    /// </summary>
    [TestMethod]
    public void TheOptionCommandSwitchesTheContentOnEveryServer()
    {
        var redis = Environment.GetEnvironmentVariable("OPENWORKER_TEST_REDIS");

        if (string.IsNullOrEmpty(redis))
        {
            Assert.Inconclusive("OPENWORKER_TEST_REDIS is not set.");
        }

        var community = new MemoryCommunity();
        var store = new MemoryContentStore();

        using var district = new Host(community, redis);
        using var maze = new Host(community, redis);
        using var alice = district.Server.Join(1, "Alice");
        using var bob = maze.Server.Join(2, "Bob");
        using var services = new ServiceCollection()
            .AddSingleton<IServerContentStore>(store)
            .AddSingleton(district.Changes)
            .BuildServiceProvider();

        ICommand command = new OptionCommand(services);
        var done = false;

        district.Server.Run(async () => done = await command.TryExecute(alice.Entity, ["3", "true"]));

        Assert.IsTrue(done);
        Assert.IsTrue(store.Rows[ServerContentIndex.Ranking]);

        foreach (var (host, player) in new[] { (district, alice), (maze, bob) })
        {
            var frame = player.Read(2000).Single();

            Assert.AreEqual((1, 7), (frame.Group, frame.Command));
            CollectionAssert.AreEqual(new byte[] { 0, 0, 0, 1, 0 }, frame.Content);
            Assert.IsTrue(host.Server.Loop.InvokeAsync(() => host.Contents[ServerContentIndex.Ranking]).GetAwaiter().GetResult());
        }
    }
}
