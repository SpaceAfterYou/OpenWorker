using System.Net;
using System.Net.Sockets;
using System.Text;
using Arch.Core;
using Microsoft.Extensions.Configuration;
using OpenWorker.Channel;
using OpenWorker.DependencyInjection;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.SoulWorker.Network;
using Redis.OM;
using Redis.OM.Searching;

namespace OpenWorker.AuthServer.Test;

/// <summary>
/// Needs Redis with the search module: set OPENWORKER_TEST_REDIS, for example redis://127.0.0.1:6379.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class TestLeaveManager
{
    private const string AuthHost = "10.0.0.1";
    private const short AuthPort = 10_000;
    private const string GateHost = "10.0.0.2";
    private const short GatePort = 10_010;

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
        await provider.Connection.CreateIndexAsync(typeof(GateCache));
    }

    private readonly record struct Answer(int Actor, int Account, string Address, short Port, byte Destination, bool Result);

    private sealed class Harness : IAsyncDisposable
    {
        private readonly TcpClient _client = new();
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly BinaryReader _reader;

        public World World { get; } = World.Create();
        public Entity Player { get; }
        public int Account { get; } = Random.Shared.Next(1_000_000, int.MaxValue);
        public int Person { get; } = Random.Shared.Next(1, 1_000_000);
        public short Gate { get; } = (short)Random.Shared.Next(1_000, short.MaxValue);
        public SessionValue Session { get; }
        public SessionLeaseManager Leases { get; } = new(Sessions);
        public LeaveManager Leaving { get; }

        public Harness()
        {
            Session = new SessionValue(Account);

            _listener.Start();
            _client.Connect(IPAddress.Loopback, ((IPEndPoint)_listener.LocalEndpoint).Port);
            _client.ReceiveTimeout = 2_000;

            Player = World.Create(
                new ServerSessionComponent(_listener.AcceptTcpClient()),
                new ClaimsComponent(),
                new ActorComponent { Identifier = Person, Type = ActorType.User });

            _reader = new BinaryReader(_client.GetStream());

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Gate:Id"] = Gate.ToString(),
                    ["Auth:Host"] = AuthHost,
                    ["Auth:Port"] = AuthPort.ToString()
                })
                .Build();

            Leaving = new LeaveManager(World, configuration, Gates, Leases);
        }

        public async Task SignInAsync()
        {
            World.Set(Player, new ClaimsComponent(Session));

            await Leases.AttachAsync(Player, Session.Key, Account);
        }

        public Answer ReadAnswer()
        {
            var header = new MessageHeader(_reader);
            var payload = _reader.ReadBytes(header.ContentLength);

            MessageEncryption.Exchange(payload.AsSpan(), header.Version);

            using var reader = new BinaryReader(new MemoryStream(payload));

            Assert.AreEqual((byte)3, reader.ReadByte());
            Assert.AreEqual((byte)0x60, reader.ReadByte());

            var actor = reader.ReadInt32();
            var account = reader.ReadInt32();
            var address = Encoding.ASCII.GetString(reader.ReadBytes(reader.ReadUInt16()));
            var port = reader.ReadInt16();
            var destination = reader.ReadByte();
            var result = reader.ReadBoolean();

            Assert.AreEqual(payload.Length, reader.BaseStream.Position, "The whole response must be read.");

            return new Answer(actor, account, address, port, destination, result);
        }

        public bool HasAnswer()
        {
            Thread.Sleep(100);

            return _client.Available > 0;
        }

        public async ValueTask DisposeAsync()
        {
            _reader.Dispose();
            _client.Dispose();
            _listener.Stop();
            _listener.Dispose();
            World.Dispose();

            await ValueTask.CompletedTask;
        }
    }

    [TestMethod]
    public async Task ServerListPointsToAuthAndAllowsResuming()
    {
        await using var harness = new Harness();

        await harness.SignInAsync();

        await harness.Leaving.LeaveAsync(harness.Player, LeaveDestination.ServerList);

        var answer = harness.ReadAnswer();

        Assert.AreEqual(new Answer(harness.Person, harness.Account, AuthHost, AuthPort, 0, true), answer);

        var session = await Sessions.FindByIdAsync(harness.Session.Key.ToString());

        Assert.IsNotNull(session);
        Assert.IsTrue(session.ReturnToAuth);
    }

    [TestMethod]
    public async Task CharacterSelectPointsToTheGateOfThePlayer()
    {
        await using var harness = new Harness();

        await Gates.InsertAsync(new GateCache
        {
            Identifier = harness.Gate,
            OnlineCount = 0,
            Workload = default,
            Host = GateHost,
            Port = GatePort
        }, TimeSpan.FromMinutes(1));

        await harness.SignInAsync();

        await harness.Leaving.LeaveAsync(harness.Player, LeaveDestination.CharacterSelect);

        var answer = harness.ReadAnswer();

        Assert.AreEqual(new Answer(harness.Person, harness.Account, GateHost, GatePort, 1, true), answer);

        var session = await Sessions.FindByIdAsync(harness.Session.Key.ToString());

        Assert.IsNotNull(session);
        Assert.IsFalse(session.ReturnToAuth, "The auth server must not resume a session that went to the gate.");
    }

    [TestMethod]
    public async Task CharacterSelectIsRefusedWithoutARunningGate()
    {
        await using var harness = new Harness();

        await harness.SignInAsync();

        await harness.Leaving.LeaveAsync(harness.Player, LeaveDestination.CharacterSelect);

        var answer = harness.ReadAnswer();

        Assert.IsFalse(answer.Result);
        Assert.IsGreaterThan(0, answer.Address.Length, "The client treats an empty address as a broken packet.");
    }

    [TestMethod]
    public async Task UnknownDestinationIsRefused()
    {
        await using var harness = new Harness();

        await harness.SignInAsync();

        await harness.Leaving.LeaveAsync(harness.Player, (LeaveDestination)7);

        Assert.IsFalse(harness.ReadAnswer().Result);
    }

    [TestMethod]
    public async Task ConnectionWithoutLoginGetsNoAddress()
    {
        await using var harness = new Harness();

        await harness.Leaving.LeaveAsync(harness.Player, LeaveDestination.ServerList);

        Assert.IsFalse(harness.HasAnswer());
    }
}
