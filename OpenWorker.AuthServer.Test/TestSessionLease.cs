using System.Net;
using System.Net.Sockets;
using Arch.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenWorker.AuthServer.App;
using OpenWorker.AuthServer.Test.DependencyInjection;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.DependencyInjection;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Enums;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Login.Requests;
using OpenWorker.Hotspot.SoulWorker.Network;
using OpenWorker.Persistence;
using OpenWorker.Persistence.Utils;
using Redis.OM;
using Redis.OM.Searching;
using StackExchange.Redis;
using StackExchange.Redis.Profiling;

namespace OpenWorker.AuthServer.Test;

/// <summary>
/// Needs Redis with the search module: set OPENWORKER_TEST_REDIS, for example redis://127.0.0.1:6379.
/// </summary>
[TestClass]
public sealed class TestSessionLease
{
    private static RedisConnectionProvider Provider { get; set; } = null!;
    private static ConnectionMultiplexer Redis { get; set; } = null!;
    private static IRedisCollection<SessionCache> Sessions { get; set; } = null!;

    private static int _nextAccount = Random.Shared.Next(100_000, 1_000_000);

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext _)
    {
        var connection = Environment.GetEnvironmentVariable("OPENWORKER_TEST_REDIS");

        if (string.IsNullOrEmpty(connection))
        {
            Assert.Inconclusive("OPENWORKER_TEST_REDIS is not set.");
        }

        Redis = await ConnectionMultiplexer.ConnectAsync(new RedisOptions { ConnectionString = connection }.ToConfigurationOptions());
        Provider = new RedisConnectionProvider(Redis);
        Sessions = Cache.CreateCollection<SessionCache>(Provider);

        await Provider.Connection.CreateIndexAsync(typeof(SessionCache));
    }

    private static async Task<double> TimeToLiveAsync(long session)
    {
        var result = await Provider.Connection.ExecuteAsync("TTL", $"{typeof(SessionCache).FullName}:{session}");

        return result;
    }

    private static Task<SessionCache?> FindAsync(long session)
    {
        return Sessions.FindByIdAsync(session.ToString());
    }

    private static long NewSession()
    {
        return Random.Shared.NextInt64(1, long.MaxValue);
    }

    [TestMethod]
    public async Task AttachedSessionIsConnectedWithExpiry()
    {
        using var world = World.Create();
        var leases = new SessionLeaseManager(Sessions, Redis);
        var session = NewSession();

        await leases.AttachAsync(world.Create(), session, 1);

        var value = await FindAsync(session);

        Assert.IsNotNull(value);
        Assert.IsTrue(value.Connected);
        Assert.IsGreaterThan(SessionLeaseManager.DisconnectedLease.TotalSeconds, await TimeToLiveAsync(session));
        Assert.IsLessThanOrEqualTo(SessionLeaseManager.ConnectedLease.TotalSeconds, await TimeToLiveAsync(session));
    }

    [TestMethod]
    public async Task ClosedConnectionKeepsSessionForTheNextServer()
    {
        using var world = World.Create();
        var leases = new SessionLeaseManager(Sessions, Redis);
        var player = world.Create();
        var session = NewSession();

        await leases.AttachAsync(player, session, 1);
        await leases.OnSessionClosedAsync(player);

        var value = await FindAsync(session);

        Assert.IsNotNull(value, "A move between servers must find the session.");
        Assert.IsFalse(value.Connected);

        var ttl = await TimeToLiveAsync(session);

        Assert.IsGreaterThan(0, ttl);
        Assert.IsLessThanOrEqualTo(SessionLeaseManager.DisconnectedLease.TotalSeconds, ttl);
    }

    [TestMethod]
    public async Task LateCloseOfThePreviousServerDoesNotReleaseTheNewOne()
    {
        using var world = World.Create();
        var previous = new SessionLeaseManager(Sessions, Redis);
        var next = new SessionLeaseManager(Sessions, Redis);
        var session = NewSession();
        var previousPlayer = world.Create();

        await previous.AttachAsync(previousPlayer, session, 1);
        await next.AttachAsync(world.Create(), session, 1);
        await previous.OnSessionClosedAsync(previousPlayer);

        var value = await FindAsync(session);

        Assert.IsNotNull(value);
        Assert.IsTrue(value.Connected);
    }

    [TestMethod]
    public async Task RenewExtendsOnlyOwnedLeasesInOneRoundTrip()
    {
        using var world = World.Create();
        var owner = new SessionLeaseManager(Sessions, Redis);
        var sessions = Enumerable.Range(0, 50).Select(_ => NewSession()).ToArray();
        var session = sessions[0];

        foreach (var each in sessions)
        {
            await owner.AttachAsync(world.Create(), each, 1);
        }

        // The first renewal loads the script into Redis; the ones after send its hash.
        await owner.RenewAsync();

        foreach (var each in sessions)
        {
            await Provider.Connection.ExecuteAsync("EXPIRE", $"{typeof(SessionCache).FullName}:{each}", "5");
        }

        var profiling = new ProfilingSession();
        Redis.RegisterProfiler(() => profiling);

        var renewed = await owner.RenewAsync();

        Redis.RegisterProfiler(() => null);

        var commands = profiling.FinishProfiling().Select(e => e.Command).ToArray();

        Assert.AreEqual(sessions.Length, renewed);
        Assert.HasCount(1, commands, "fifty leases, one command");
        Assert.AreEqual("EVALSHA", commands[0]);

        foreach (var each in sessions)
        {
            Assert.IsGreaterThan(5, await TimeToLiveAsync(each));
        }

        var other = new SessionLeaseManager(Sessions, Redis);

        await other.AttachAsync(world.Create(), session, 1);
        var taken = (await FindAsync(session))!.Owner;

        await owner.RenewAsync();

        Assert.AreEqual(taken, (await FindAsync(session))!.Owner);
    }

    [TestMethod]
    public async Task ReturnToAuthSurvivesCloseAndIsClearedByTheNextAttach()
    {
        using var world = World.Create();
        var gate = new SessionLeaseManager(Sessions, Redis);
        var auth = new SessionLeaseManager(Sessions, Redis);
        var player = world.Create();
        var session = NewSession();

        await gate.AttachAsync(player, session, 1);
        await gate.MarkReturnToAuthAsync(player);
        await gate.OnSessionClosedAsync(player);

        Assert.IsTrue((await FindAsync(session))!.ReturnToAuth);

        await auth.AttachAsync(world.Create(), session, 1);

        Assert.IsFalse((await FindAsync(session))!.ReturnToAuth);
    }

    /// <summary>
    /// The request that continues a session at the auth server names the account and nothing secret, so the session is
    /// continued only from the address its player left the game from.
    /// </summary>
    [TestMethod]
    public async Task ASessionOnItsWayBackIsContinuedOnlyFromTheAddressItLeftFrom()
    {
        using var world = World.Create();
        var district = new SessionLeaseManager(Sessions, Redis);
        var player = world.Create();
        var session = NewSession();

        await district.AttachAsync(player, session, 1);

        Assert.IsFalse(SessionLeaseManager.MayReturn((await FindAsync(session))!, "203.0.113.7:50000"), "Not on its way back yet");

        await district.MarkReturnToAuthAsync(player, "203.0.113.7:50000");
        await district.OnSessionClosedAsync(player);

        var record = (await FindAsync(session))!;

        Assert.IsTrue(SessionLeaseManager.MayReturn(record, "203.0.113.7:61234"), "The same address, another port");
        Assert.IsFalse(SessionLeaseManager.MayReturn(record, "198.51.100.9:50000"), "Another address");
        Assert.IsFalse(SessionLeaseManager.MayReturn(record, null), "An address that is not known");
    }

    /// <summary>
    /// A person is in one world at a time: a server waits for the district or the maze that holds the session to let the
    /// person go, and follows a login or a gate server at once.
    /// </summary>
    [TestMethod]
    public async Task AServerWaitsForTheWorldThatHoldsTheSessionAndFollowsAGateAtOnce()
    {
        using var world = World.Create();
        var play = Options.Create(new SessionLeaseOptions { Play = true });
        var gate = new SessionLeaseManager(Sessions, Redis);
        var district = new SessionLeaseManager(Sessions, Redis, options: play);
        var maze = new SessionLeaseManager(Sessions, Redis, options: play);
        var session = NewSession();
        var atGate = world.Create();
        var inDistrict = world.Create();
        var inMaze = world.Create();

        Assert.IsFalse(await district.TakeOverAsync(inDistrict, session, 1), "No such session");

        await gate.AttachAsync(atGate, session, 1);

        Assert.IsTrue(await district.TakeOverAsync(inDistrict, session, 1), "A gate is followed at once");

        var entering = maze.TakeOverAsync(inMaze, session, 1);

        await Task.Delay(TimeSpan.FromMilliseconds(400));

        Assert.IsFalse(entering.IsCompleted, "The district still has the person");

        await district.OnSessionClosedAsync(inDistrict);

        Assert.IsTrue(await entering, "The district has let the person go");
        Assert.IsTrue((await FindAsync(session))!.Connected);
    }

    /// <summary>
    /// The database of the test: the first write waits until the test lets it through.
    /// </summary>
    private sealed class HeldStore : IPersonStore
    {
        public TaskCompletionSource<IReadOnlyList<PersonChanges>> Writing { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Written { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task SaveChangesAsync(IReadOnlyList<PersonChanges> batch, CancellationToken cancellationToken = default)
        {
            Writing.TrySetResult(batch);

            await Written.Task;
        }

        public Task<List<ItemPersistent>> LoadItemsAsync(int person, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<long[]> ReserveSerialsAsync(int count, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<string?> LoadDocumentAsync(int person, PersonStateKind kind, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    /// <summary>
    /// The person stands somewhere it has not been written at, so its flush has a write to make.
    /// </summary>
    private sealed class NewPlace : IPersonPlaceSource
    {
        public static PersonPlace Place { get; } = new(10003, 1, 2, 3, 0);

        public bool TryGetPlace(World world, Entity player, out PersonPlace place)
        {
            place = Place;

            return true;
        }
    }

    /// <summary>
    /// A connection that closes gives its session up only after what the person changed is in the database: the server the
    /// person goes to (or a new login) reads the person only then. The lease is registered before the flush, as every
    /// server registers it, and still goes last.
    /// </summary>
    [TestMethod]
    public async Task AClosedConnectionGivesTheSessionUpOnlyAfterItsLastWriteIsInTheDatabase()
    {
        var store = new HeldStore();
        var play = Options.Create(new SessionLeaseOptions { Play = true });
        var session = NewSession();

        await using var server = await ServerHarness.StartAsync(collection =>
        {
            collection.AddSingleton(provider => new SessionLeaseManager(
                Sessions, Redis, provider.GetRequiredService<World>(), provider.GetRequiredService<WorldLoop>(), play));
            collection.AddSingleton<ISessionCloseHandler>(provider => provider.GetRequiredService<SessionLeaseManager>());

            collection.AddSingleton<IPersonStore>(store);
            collection.AddSingleton<IPersonPlaceSource, NewPlace>();
            collection.AddSingleton<PersonSnapshots>();
            collection.AddSingleton<PersistenceWriter>();
            collection.AddHostedService(provider => provider.GetRequiredService<PersistenceWriter>());
            collection.AddSingleton<PersonFlush>();
            collection.AddSingleton<ISessionCloseHandler>(provider => provider.GetRequiredService<PersonFlush>());
        }, cancellationToken: TestContext.CancellationToken);

        var client = await server.ConnectAsync(TestContext.CancellationToken);

        // The person has entered: its state is loaded and the session is held here.
        var player = await server.Loop.InvokeAsync(() =>
        {
            Entity found = default;

            server.World.Query(new QueryDescription().WithAll<ServerSessionComponent>(), entity => found = entity);

            server.World.Add(found, new DirtyComponent(new PersonDirty(1)));

            return found;
        });

        await server.Host.Services.GetRequiredService<SessionLeaseManager>().AttachAsync(player, session, 1);

        client.Dispose();

        var batch = await store.Writing.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);

        Assert.AreEqual(NewPlace.Place, batch.Single().Place);

        // The write has not reached the database: the session is still this server's, and the next one waits for it.
        Assert.IsTrue((await FindAsync(session))!.Connected, "A new login is refused while the write is on its way");

        using var world = World.Create();
        var next = new SessionLeaseManager(Sessions, Redis, options: play);
        var entering = next.TakeOverAsync(world.Create(), session, 1);

        await Task.Delay(TimeSpan.FromMilliseconds(400), TestContext.CancellationToken);

        Assert.IsFalse(entering.IsCompleted, "The next server waits for the write");

        store.Written.SetResult();

        Assert.IsTrue(await entering.WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken), "The written person is let go");
    }

    /// <summary>
    /// A name is not tried any more after its wrong passwords, and a right one before that forgets them.
    /// </summary>
    [TestMethod]
    public async Task ANameIsClosedAfterItsWrongPasswordsAndARightOneForgetsThem()
    {
        var attempts = new LoginAttempts(Redis);
        var name = "Guess" + Guid.NewGuid().ToString("N");

        for (var index = 0; index < LoginAttempts.Limit - 1; index++)
        {
            await attempts.FailedAsync(name);
        }

        Assert.IsFalse(await attempts.IsClosedAsync(name), "One try is left");

        await attempts.SucceededAsync(name);

        for (var index = 0; index < LoginAttempts.Limit; index++)
        {
            await attempts.FailedAsync(name.ToUpperInvariant());
        }

        Assert.IsTrue(await attempts.IsClosedAsync(name), "The name is one, however it is written");

        await attempts.SucceededAsync(name);
    }

    [TestMethod]
    public async Task SecondLoginIsRejectedWhileConnectedAndReplacesAfterDisconnect()
    {
        var account = Interlocked.Increment(ref _nextAccount);

        PasswordHash.Create("password", out var hash, out var salt);

        var databaseName = Guid.NewGuid().ToString();

        var factory = new ServiceCollection()
            .AddDbContextFactory<PersistenceContext>(o => o.UseInMemoryDatabase(databaseName))
            .BuildServiceProvider()
            .GetRequiredService<IDbContextFactory<PersistenceContext>>();

        await using (var database = await factory.CreateDbContextAsync())
        {
            database.Accounts.Add(new AccountPersistent
            {
                Id = account,
                Username = $"user{account}",
                PasswordHash = hash,
                SaltHash = salt
            });

            await database.SaveChangesAsync();
        }

        using var world = World.Create();
        var leases = new SessionLeaseManager(Sessions, Redis);

        var gameplay = new LoginGameplay(
            Sessions,
            factory,
            world,
            Options.Create(new AuthOptions()),
            NullLogger<LoginGameplay>.Instance,
            leases);

        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        async Task<(Entity Player, LoginErrorMessageCode Code, TcpClient Client)> LoginAsync()
        {
            var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);

            // The login sets the claims of the connection: the entity has them, as every connection of a server has.
            var player = world.Create(new ServerSessionComponent(await listener.AcceptTcpClientAsync()), new ClaimsComponent());

            await gameplay.TryJoinAsync(
                new ServiceHandleContext(player, CancellationToken.None),
                new LoginAuthRequest($"user{account}", "password", "00-00-00-00-00-00"));

            var reader = new BinaryReader(client.GetStream());
            var header = new MessageHeader(reader);
            var payload = reader.ReadBytes(header.ContentLength);
            MessageEncryption.Exchange(payload.AsSpan(), header.Version);

            using var body = new BinaryReader(new MemoryStream(payload));

            body.ReadBytes(2 + 4 + 1 + 18);
            body.ReadBytes(body.ReadInt16());

            return (player, (LoginErrorMessageCode)body.ReadInt32(), client);
        }

        var first = await LoginAsync();
        Assert.AreEqual(LoginErrorMessageCode.None, first.Code);

        var firstSession = (await Sessions.Where(e => e.Account == account).ToListAsync()).Single().Session;

        var second = await LoginAsync();
        Assert.AreEqual(LoginErrorMessageCode.InGameAlready, second.Code);

        await leases.OnSessionClosedAsync(first.Player);

        var third = await LoginAsync();
        Assert.AreEqual(LoginErrorMessageCode.None, third.Code);

        var active = await Sessions.Where(e => e.Account == account).ToListAsync();

        Assert.HasCount(1, active);
        Assert.AreNotEqual(firstSession, active.Single().Session);

        first.Client.Dispose();
        second.Client.Dispose();
        third.Client.Dispose();
    }

    [TestMethod]
    public async Task SharedCollectionSurvivesConcurrentReads()
    {
        var keys = Enumerable.Range(0, 200).Select(_ => Random.Shared.NextInt64(1, long.MaxValue)).ToArray();

        foreach (var key in keys)
        {
            await Sessions.InsertAsync(new SessionCache
            {
                Session = key,
                Account = Interlocked.Increment(ref _nextAccount),
                UpdatedAt = DateTime.UtcNow,
                Connected = false,
                Owner = string.Empty
            }, TimeSpan.FromSeconds(30));
        }

        await Parallel.ForEachAsync(
            Enumerable.Range(0, 40),
            new ParallelOptions { MaxDegreeOfParallelism = 16 },
            async (_, _) =>
            {
                foreach (var key in keys)
                {
                    Assert.IsNotNull(await Sessions.FindByIdAsync(key.ToString()));
                }
            });
    }

    public TestContext TestContext { get; set; } = null!;
}
