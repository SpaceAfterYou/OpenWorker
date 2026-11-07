using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using Arch.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Channel;
using OpenWorker.DependencyInjection;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Types;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.GateServer.Gameplay;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Login.Requests;
using OpenWorker.Hotspot.Modules.Persons.Requests;
using OpenWorker.Hotspot.SoulWorker.Network;
using OpenWorker.Persistence;
using OpenWorker.UpdateContent.Res.Rows;
using Redis.OM;
using Redis.OM.Searching;

namespace OpenWorker.AuthServer.Test;

/// <summary>
/// Needs Redis with the search module: set OPENWORKER_TEST_REDIS, for example redis://127.0.0.1:6379.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class TestGateSession
{
    private const short GateId = 1;
    private const short OtherGateId = 2;

    private static IRedisCollection<SessionCache> Sessions { get; set; } = null!;
    private static IRedisCollection<GateCache> Gates { get; set; } = null!;

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext _)
    {
        var connection = Environment.GetEnvironmentVariable("OPENWORKER_TEST_REDIS");

        if (string.IsNullOrEmpty(connection))
        {
            Assert.Inconclusive("OPENWORKER_TEST_REDIS is not set.");
        }

        var provider = new RedisConnectionProvider(connection);

        Sessions = Cache.CreateCollection<SessionCache>(provider);
        Gates = Cache.CreateCollection<GateCache>(provider);

        await provider.Connection.CreateIndexAsync(typeof(SessionCache));
    }

    private sealed class Harness : IAsyncDisposable
    {
        private readonly TcpClient _client = new();
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly BinaryReader _reader;
        private readonly List<TcpClient> _others = [];

        public World World { get; } = World.Create();
        public Entity Player { get; }
        public int Account { get; } = Random.Shared.Next(1_000_000, int.MaxValue);
        public SessionValue Session { get; }
        public IDbContextFactory<PersistenceContext> Factory { get; }
        public PersonRegistry Registry { get; }
        public LoginGameplay Login { get; }
        public PersonGameplay Person { get; }

        public Harness()
        {
            Session = new SessionValue(Account);

            _listener.Start();
            _client.Connect(IPAddress.Loopback, ((IPEndPoint)_listener.LocalEndpoint).Port);

            Player = World.Create(
                new ServerSessionComponent(_listener.AcceptTcpClient()),
                new ClaimsComponent(),
                new SecondPasswordComponent { Password = string.Empty },
                new TradePasswordComponent { Password = string.Empty },
                new GatePersonComponent());

            _reader = new BinaryReader(_client.GetStream());

            var name = Guid.NewGuid().ToString();

            Factory = new ServiceCollection()
                .AddDbContextFactory<PersistenceContext>(o => o.UseInMemoryDatabase(name))
                .BuildServiceProvider()
                .GetRequiredService<IDbContextFactory<PersistenceContext>>();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Gate:Id"] = GateId.ToString() })
                .Build();

            var leases = new SessionLeaseManager(Sessions);
            var secondPassword = new SecondPasswordGameplay(World, Factory, new ServerContent([]));

            var catalog = new ItemCatalog(
                new ReadOnlyCollection<ItemRow>([]),
                new ReadOnlyCollection<ItemClassifyRow>([]),
                new ReadOnlyCollection<InvenSlotExtendRow>([]),
                new ReadOnlyCollection<BankSlotExtendRow>([]));

            var inventories = new InventoryFactory(catalog, new ReadOnlyCollection<CreateClothRow>([]), new LocalItemSerials());

            Registry = new PersonRegistry(World, inventories);

            Login = new LoginGameplay(
                World,
                Sessions,
                leases,
                Factory,
                Registry,
                secondPassword,
                configuration,
                NullLogger<LoginGameplay>.Instance);

            Person = new PersonGameplay(
                World,
                Factory,
                Registry,
                new InventoryPersistence(World, Factory, inventories, NullLogger<InventoryPersistence>.Instance),
                configuration,
                NullLogger<PersonGameplay>.Instance,
                secondPassword,
                new LeaveManager(World, configuration, Gates, leases),
                (WorldManager)null!,
                new PersonCreationCatalog(
                    new ReadOnlyCollection<CharacterInfoRow>([]),
                    new ReadOnlyCollection<CustomizeHairRow>([]),
                    new ReadOnlyCollection<CustomizeEyesRow>([]),
                    new ReadOnlyCollection<CustomizeSkinRow>([])));
        }

        public async Task<(int ActivePerson, int OtherPerson)> SeedAsync()
        {
            await using var database = await Factory.CreateDbContextAsync();

            var account = new AccountPersistent
            {
                Id = Account,
                Username = $"user{Account}",
                PasswordHash = [],
                SaltHash = []
            };

            var gate = new GatePersistent { Id = GateId };
            var other = new GatePersistent { Id = OtherGateId };

            var active = CreatePerson("Active", account, gate);
            var hidden = CreatePerson("Other", account, other);

            database.AddRange(account, gate, other, active, hidden);

            await database.SaveChangesAsync();

            return (active.Id, hidden.Id);
        }

        public static PersonPersistent CreatePerson(string name, AccountPersistent account, GatePersistent gate)
        {
            return new PersonPersistent
            {
                Name = name,
                Hero = (Hero)1,
                DefaultHairStyle = 0,
                DefaultHairColor = 0,
                DefaultEyeColor = 0,
                DefaultSkinColor = 0,
                EquippedHairStyle = 0,
                EquippedHairColor = 0,
                EquippedEyeColor = 0,
                EquippedSkinColor = 0,
                Location = 10003,
                PositionX = 0,
                PositionY = 0,
                PositionZ = 0,
                RotationX = 0,
                FatiguePointCommon = 0,
                Account = account,
                Gate = gate
            };
        }

        public LoginEnterServerRequest CreateEnterRequest(int account)
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            writer.Write(account);
            writer.Write(GateId);
            writer.Write(Session.Key);
            writer.Flush();
            stream.Position = 0;

            return new LoginEnterServerRequest(new BinaryReader(stream));
        }

        public ServiceHandleContext Context => new(Player, CancellationToken.None);

        /// <summary>
        /// Another connection in the same world, the way a real server has them.
        /// </summary>
        public Entity ConnectAnotherPlayer()
        {
            _others.Add(new TcpClient());
            _others[^1].Connect(IPAddress.Loopback, ((IPEndPoint)_listener.LocalEndpoint).Port);

            return World.Create(
                new ServerSessionComponent(_listener.AcceptTcpClient()),
                new ClaimsComponent(),
                new SecondPasswordComponent { Password = string.Empty },
                new TradePasswordComponent { Password = string.Empty },
                new GatePersonComponent());
        }

        public byte[] ReadPayload()
        {
            var header = new MessageHeader(_reader);
            var payload = _reader.ReadBytes(header.ContentLength);
            MessageEncryption.Exchange(payload.AsSpan(), header.Version);

            return payload;
        }

        public int[] VisiblePersons()
        {
            return World.Get<GatePersonComponent>(Player).SlotList
                .Where(e => e != Entity.Null)
                .Select(e => World.Get<ActorComponent>(e).Identifier)
                .ToArray();
        }

        public async ValueTask DisposeAsync()
        {
            _reader.Dispose();
            _client.Dispose();
            _others.ForEach(e => e.Dispose());
            _listener.Stop();
            _listener.Dispose();
            World.Dispose();

            await ValueTask.CompletedTask;
        }
    }

    private static async Task ReserveSessionAsync(Harness harness)
    {
        await new SessionLeaseManager(Sessions).AttachAsync(default, harness.Session.Key, harness.Account);
    }

    [TestMethod]
    public async Task OnlyPersonsOfThisGateAreLoaded()
    {
        await using var harness = new Harness();

        var (active, _) = await harness.SeedAsync();
        await ReserveSessionAsync(harness);

        await harness.Login.TryJoinAsync(harness.Context, harness.CreateEnterRequest(harness.Account));

        CollectionAssert.AreEqual(new[] { active }, harness.VisiblePersons());
    }

    [TestMethod]
    public async Task LastSelectedPersonIsRestored()
    {
        await using var harness = new Harness();

        var (active, _) = await harness.SeedAsync();

        await using (var database = await harness.Factory.CreateDbContextAsync())
        {
            var account = await database.Accounts.FirstAsync(e => e.Id == harness.Account);
            var gate = await database.Gates.FirstAsync(e => e.Id == GateId);

            var newer = Harness.CreatePerson("Newer", account, gate);
            newer.LastSelectedAt = DateTime.UtcNow.AddHours(-2);

            database.Add(newer);

            (await database.Persons.FirstAsync(e => e.Id == active)).LastSelectedAt = DateTime.UtcNow;

            await database.SaveChangesAsync();
        }

        await ReserveSessionAsync(harness);
        await harness.Login.TryJoinAsync(harness.Context, harness.CreateEnterRequest(harness.Account));

        var list = harness.World.Get<GatePersonComponent>(harness.Player);

        Assert.AreEqual(active, harness.World.Get<ActorComponent>(list.Last).Identifier);
    }

    [TestMethod]
    public async Task NoPersonIsMarkedAsLastBeforeTheFirstSelection()
    {
        await using var harness = new Harness();

        await harness.SeedAsync();
        await ReserveSessionAsync(harness);
        await harness.Login.TryJoinAsync(harness.Context, harness.CreateEnterRequest(harness.Account));

        Assert.IsFalse(harness.World.Get<GatePersonComponent>(harness.Player).HasLast);
    }

    [TestMethod]
    public async Task EveryPlayerKeepsItsOwnPersonList()
    {
        await using var harness = new Harness();

        var (active, _) = await harness.SeedAsync();
        await ReserveSessionAsync(harness);

        var players = Enumerable.Range(0, 4).Select(_ => harness.ConnectAnotherPlayer()).ToArray();

        foreach (var player in players)
        {
            await harness.Login.TryJoinAsync(
                new ServiceHandleContext(player, CancellationToken.None),
                harness.CreateEnterRequest(harness.Account));
        }

        foreach (var player in players)
        {
            var list = harness.World.Get<GatePersonComponent>(player).SlotList;

            Assert.IsNotNull(list);
            Assert.AreEqual(active, harness.World.Get<ActorComponent>(list[0]).Identifier);
        }
    }

    [TestMethod]
    public async Task SessionOfAnotherAccountIsRejected()
    {
        await using var harness = new Harness();

        await harness.SeedAsync();
        await ReserveSessionAsync(harness);

        await harness.Login.TryJoinAsync(harness.Context, harness.CreateEnterRequest(harness.Account + 1));

        var payload = harness.ReadPayload();

        Assert.AreEqual(1, payload[2], "The response must report an error.");
        Assert.AreEqual(0, harness.World.Get<ClaimsComponent>(harness.Player).Account);
    }

    [TestMethod]
    public async Task ClosedSessionLeavesNoPersonEntities()
    {
        await using var harness = new Harness();

        await harness.SeedAsync();
        await ReserveSessionAsync(harness);

        var before = harness.World.Size;

        await harness.Login.TryJoinAsync(harness.Context, harness.CreateEnterRequest(harness.Account));

        Assert.IsGreaterThan(before, harness.World.Size);

        await harness.Registry.OnSessionClosedAsync(harness.Player);

        Assert.AreEqual(before, harness.World.Size);
    }

    [TestMethod]
    public async Task DeleteRemovesOwnPersonAndRefusesOtherGate()
    {
        await using var harness = new Harness();

        var (active, hidden) = await harness.SeedAsync();
        await ReserveSessionAsync(harness);

        await harness.Login.TryJoinAsync(harness.Context, harness.CreateEnterRequest(harness.Account));

        static PersonDeleteReqRequest Request(int person)
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            writer.Write(person);
            writer.Flush();
            stream.Position = 0;

            return new PersonDeleteReqRequest(new BinaryReader(stream));
        }

        await harness.Person.DeleteAsync(harness.Context, Request(hidden));

        await using (var database = await harness.Factory.CreateDbContextAsync())
        {
            Assert.IsTrue(await database.Persons.AnyAsync(e => e.Id == hidden));
        }

        await harness.Person.DeleteAsync(harness.Context, Request(active));

        await using (var database = await harness.Factory.CreateDbContextAsync())
        {
            Assert.IsFalse(await database.Persons.AnyAsync(e => e.Id == active));
        }

        Assert.IsEmpty(harness.VisiblePersons());
    }
}
