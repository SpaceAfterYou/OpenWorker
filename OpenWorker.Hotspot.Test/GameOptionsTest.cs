using System.Collections.Concurrent;
using Arch.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenWorker.Channel.Community;
using OpenWorker.Commands;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Stores;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Modules.Attendance;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Gameplay.Modules.Rewards;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Enums;
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

    /// <summary>
    /// The server where the command is given has the switch, and its players have been told, before the command returns: a
    /// request right after it sees the content. Its own switch coming back from Redis changes nothing, even after a later one.
    /// </summary>
    [TestMethod]
    public void TheServerOfTheCommandHasTheSwitchBeforeTheCommandReturns()
    {
        var redis = Environment.GetEnvironmentVariable("OPENWORKER_TEST_REDIS");

        if (string.IsNullOrEmpty(redis))
        {
            Assert.Inconclusive("OPENWORKER_TEST_REDIS is not set.");
        }

        using var district = new Host(new MemoryCommunity(), redis);
        using var alice = district.Server.Join(1, "Alice");
        using var services = new ServiceCollection()
            .AddSingleton<IServerContentStore>(new MemoryContentStore())
            .AddSingleton(district.Changes)
            .BuildServiceProvider();

        ICommand command = new OptionCommand(services);
        bool on = false, off = true;

        district.Server.Run(async () =>
        {
            await command.TryExecute(alice.Entity, ["3", "true"]);
            on = district.Contents[ServerContentIndex.Ranking];

            await command.TryExecute(alice.Entity, ["3", "false"]);
            off = district.Contents[ServerContentIndex.Ranking];
        });

        Assert.IsTrue(on);
        Assert.IsFalse(off);

        // Both echoes come back meanwhile; neither switches anything, so the player is told twice and the content stays off.
        var frames = alice.Read(1000);

        Assert.HasCount(2, frames);
        Assert.IsFalse(district.Server.Loop.InvokeAsync(() => district.Contents[ServerContentIndex.Ranking]).GetAwaiter().GetResult());
    }

    /// <summary>
    /// An unchanged client of a game master forces whispers, trades, parties, leagues, friends and looks to "nobody" when it
    /// reads its record and sends that at once: those first options keep what the player chose. Options sent later, and an
    /// own "nobody" of a client that does not force, are the player's choice and are taken.
    /// </summary>
    [TestMethod]
    public void OnlyTheOptionsAGameMastersClientForcesAtItsRecordKeepTheChoice()
    {
        using var world = World.Create();

        var master = world.Create(new AccessComponent(AccessLevel.GameMaster), new ForcedStaffOptionsComponent());
        var player = world.Create(new AccessComponent(AccessLevel.None), new ForcedStaffOptionsComponent());

        StaffOptions.ExpectForced(world, master);
        StaffOptions.ExpectForced(world, player);

        Assert.IsTrue(StaffOptions.TakeExpected(world, master), "The first options after the record may be forced.");
        Assert.IsFalse(StaffOptions.TakeExpected(world, master), "The next ones are the player's.");
        Assert.IsFalse(StaffOptions.TakeExpected(world, player), "A player's client forces nothing.");

        var kept = Enumerable.Repeat((byte)'0', 64).ToArray();
        kept[0] = (byte)'5';
        kept[PersonOptions.FriendRequest] = (byte)'1';
        kept[PersonOptions.OtherInfo] = (byte)'1';

        var forced = kept.ToArray();
        "222202"u8.CopyTo(forced.AsSpan(PersonOptions.Whisper));
        forced[3] = (byte)'1';

        var options = PersonOptions.KeepChoiceOfStaff(forced, kept);

        CollectionAssert.AreEqual("000011"u8.ToArray(), options[PersonOptions.Whisper..(PersonOptions.OtherInfo + 1)]);
        Assert.AreEqual((byte)'1', options[3]);

        var own = kept.ToArray();
        own[PersonOptions.Whisper] = (byte)'2';

        options = PersonOptions.KeepChoiceOfStaff(own, kept);

        Assert.AreEqual(PersonOptions.Nobody, PersonOptions.Level(options, PersonOptions.Whisper));
    }
}
