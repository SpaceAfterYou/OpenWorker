using System.Buffers.Binary;
using Arch.Core;
using Microsoft.EntityFrameworkCore;
using Redis.OM;
using StackExchange.Redis;
using OpenWorker.Domain.Stores;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Messages.Response.Person.Enums;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Requests;
using OpenWorker.Persistence;
using OpenWorker.Persistence.Utils;
using OpenWorker.Server.Common.Services;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// What the servers of one login share about the trade password, against a real Redis (OPENWORKER_TEST_REDIS): a district
/// and a maze are two servers, and the player goes from one to the other and back.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class TradePasswordStatesTest
{
    private const string Variable = "OPENWORKER_TEST_REDIS";
    private const string Password = "1234";

    private static string Address()
    {
        var address = Environment.GetEnvironmentVariable(Variable);

        if (string.IsNullOrEmpty(address))
        {
            Assert.Inconclusive($"{Variable} is not set.");
        }

        return address;
    }

    /// <summary>
    /// The password is never made against a database here: the hash is made by hand.
    /// </summary>
    private sealed class NoDatabase : IDbContextFactory<PersistenceContext>
    {
        public PersistenceContext CreateDbContext() => throw new InvalidOperationException("The test needs no database.");
    }

    /// <summary>
    /// A connection to the Redis the variable names (a redis:// address, as the servers are given it).
    /// </summary>
    private static Task<ConnectionMultiplexer> ConnectAsync(string address)
    {
        var uri = new Uri(address);

        return ConnectionMultiplexer.ConnectAsync(uri.Host + ":" + uri.Port.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    private static long NewSession() => Random.Shared.NextInt64(1L << 40, long.MaxValue);

    /// <summary>
    /// A server with the player of a login who has an account with a password and has not given it yet.
    /// </summary>
    private static (TestServer Server, TestPlayer Player, TradePasswordService Service) Enter(
        ITradePasswordStates states,
        ITradePasswordTries tries,
        long session,
        int person)
    {
        var server = new TestServer(new MemoryCommunity());
        var player = server.Join(person, "person" + person);
        var service = new TradePasswordService(server.World, new NoDatabase(), states, tries);

        server.World.Add(player.Entity, new ClaimsComponent(session));
        server.World.Add(player.Entity, new TradePasswordComponent
        {
            Password = PasswordHash.CreateEncoded(Password),
            State = PasswordProtectionState.UnAuthenticated
        });

        // What the entry of a server does after it has set the person.
        server.Run(() => service.EnterAsync(player.Entity, CancellationToken.None).AsTask());

        return (server, player, service);
    }

    private static (int Error, PasswordProtectionState State) Ask(TestServer server, TestPlayer player, TradePasswordService service, string password)
    {
        player.Read(50);

        server.Run(() => service.OnHandleAsync(new ServiceHandleContext(player.Entity, CancellationToken.None), Check(password)).AsTask());

        var answer = player.Read().Single(e => e.Group == (byte)GroupOpcode.Character && e.Command == (byte)CharacterOpcode.TradePassword);

        return (BinaryPrimitives.ReadInt32LittleEndian(answer.Content), (PasswordProtectionState)answer.Content[4]);
    }

    private static PersonTradePasswordRequest Check(string password)
    {
        var bytes = new byte[1 + 2 + password.Length];

        bytes[0] = (byte)PasswordCheckType.Check;
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(1), (ushort)password.Length);
        System.Text.Encoding.ASCII.GetBytes(password, bytes.AsSpan(3));

        var reader = new PacketReader(bytes);

        return PersonTradePasswordRequest.Read(ref reader);
    }

    /// <summary>
    /// A player who gave the right password on one server has given it on the next one of the same login, and a wrong
    /// password takes it back everywhere; another login is not asked about it.
    /// </summary>
    [TestMethod]
    public async Task ThePasswordGivenOnOneServerIsGivenOnTheNextAndAWrongOneTakesItBack()
    {
        await using var redis = await ConnectAsync(Address());

        var states = new RedisTradePasswordStates(redis);
        var tries = new MemoryTradePasswordTries();
        var login = NewSession();
        var stranger = NewSession();

        try
        {
            var (district, player, service) = Enter(states, tries, login, 1);

            using (district)
            using (player)
            {
                Assert.AreEqual(PasswordProtectionState.UnAuthenticated, district.World.Get<TradePasswordComponent>(player.Entity).State);
                Assert.AreEqual((0, PasswordProtectionState.Authenticated), Ask(district, player, service, Password));
            }

            // The maze the same login goes into finds the password given; a login of its own does not.
            var (maze, inMaze, mazeService) = Enter(states, tries, login, 1);

            using (maze)
            using (inMaze)
            {
                Assert.AreEqual(PasswordProtectionState.Authenticated, maze.World.Get<TradePasswordComponent>(inMaze.Entity).State);

                // A wrong one takes the password back.
                Assert.AreEqual((TradePasswordService.WrongPassword, PasswordProtectionState.UnAuthenticated), Ask(maze, inMaze, mazeService, "7654"));
            }

            var (back, returned, _) = Enter(states, tries, login, 1);

            using (back)
            using (returned)
            {
                Assert.AreEqual(PasswordProtectionState.UnAuthenticated, back.World.Get<TradePasswordComponent>(returned.Entity).State);
            }

            var (other, newcomer, otherService) = Enter(states, tries, stranger, 2);

            using (other)
            using (newcomer)
            {
                Assert.AreEqual((0, PasswordProtectionState.Authenticated), Ask(other, newcomer, otherService, Password));
            }

            var (again, same, _) = Enter(states, tries, stranger, 2);

            using (again)
            using (same)
            {
                Assert.AreEqual(PasswordProtectionState.Authenticated, again.World.Get<TradePasswordComponent>(same.Entity).State);
            }
        }

        finally
        {
            await redis.GetDatabase().KeyDeleteAsync([RedisTradePasswordStates.Key(login), RedisTradePasswordStates.Key(stranger)]);
        }
    }

    /// <summary>
    /// The record lives as long as the lease of the session: an entry, a renewal and the close of the connection each give it
    /// the length of the lease, so it is there for the next server of the login and gone when the login is.
    /// </summary>
    [TestMethod]
    public async Task TheRecordLivesAsLongAsTheLeaseOfTheSession()
    {
        var address = Address();

        await using var redis = await ConnectAsync(address);

        var connection = new RedisConnectionProvider(address);

        await connection.Connection.CreateIndexAsync(typeof(SessionCache));

        var states = new RedisTradePasswordStates(redis);
        var leases = new SessionLeaseManager(connection.RedisCollection<SessionCache>(saveState: false), redis);
        var database = redis.GetDatabase();
        var session = NewSession();
        var key = RedisTradePasswordStates.Key(session);

        using var world = World.Create();

        var player = world.Create();

        try
        {
            await states.GiveAsync(session);

            async Task<TimeSpan> LeftAsync() => (await database.KeyTimeToLiveAsync(key))!.Value;

            Assert.IsGreaterThan(TimeSpan.FromSeconds(80), await LeftAsync(), "A right password is kept for a connected lease.");

            // Whatever shortened it, an entry on a server lengthens it again.
            await database.KeyExpireAsync(key, TimeSpan.FromSeconds(5));
            await leases.AttachAsync(player, session, 1);

            Assert.IsGreaterThan(TimeSpan.FromSeconds(80), await LeftAsync(), "The entry.");

            await database.KeyExpireAsync(key, TimeSpan.FromSeconds(5));

            Assert.AreEqual(1, await leases.RenewAsync());
            Assert.IsGreaterThan(TimeSpan.FromSeconds(80), await LeftAsync(), "The renewal.");

            // The connection closes: the next server has the length of the lease that is left to come.
            await leases.OnSessionClosedAsync(player);

            Assert.IsLessThanOrEqualTo(SessionLeaseManager.DisconnectedLease, await LeftAsync(), "The close.");
            Assert.IsGreaterThan(TimeSpan.FromSeconds(50), await LeftAsync());
        }

        finally
        {
            await database.KeyDeleteAsync(key);
            await database.KeyDeleteAsync(typeof(SessionCache).FullName + ":" + session);
        }
    }
}
