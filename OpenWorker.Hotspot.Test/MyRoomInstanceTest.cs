using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Stores;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Modules.MyRoom;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Lua;
using OpenWorker.MazeServer.Services;
using StackExchange.Redis;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A room is played in one instance at a time: the one its record in Redis names. Needs Redis: set OPENWORKER_TEST_REDIS.
/// Each test takes accounts of its own, so runs do not meet.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MyRoomInstanceTest
{
    private const short Gate = 1;

    private static readonly (Guid Server, string Host, short Port) Unregistered = (Guid.Empty, "maze", 100);

    private static MyRooms Rooms(IConnectionMultiplexer redis, Guid server)
    {
        return new MyRooms(
            new MyRoomDirectory(redis),
            Options.Create(new InstanceOptions { Identifier = server, PublicHost = "maze", PublicPort = 100 }),
            Options.Create(new MazeEndpointOptions { Host = "maze", Port = 100 }),
            NullLogger<MyRooms>.Instance);
    }

    private static MyRoomState State(int owner) => new(new MyRoomRecord(owner, Gate, 1, 1, 0, "Room"), 19, [], MyRoomTest.Catalog());

    /// <summary>
    /// The instance takes the room as the first player comes and opens it.
    /// </summary>
    private static async Task<MyRoomState> OpenAsync(MyRooms rooms, Guid instance, int owner)
    {
        var claim = await rooms.ClaimAsync(instance, owner, Gate);

        Assert.IsNotNull(claim);
        Assert.IsNull(claim.Value.Elsewhere, "the record names this instance or none");

        var room = State(owner);

        rooms.Open(instance, room, claim.Value.Holder);

        return room;
    }

    /// <summary>
    /// The sequence that made two instances of one room: the last player leaves while a visitor is on the way, the visitor
    /// arrives, and the owner comes back. The visitor's instance takes the record again, so the owner is sent into it.
    /// </summary>
    [TestMethod]
    public async Task AVisitorWhoArrivesAfterTheLastPlayerLeftKeepsTheRoomInOneInstance()
    {
        var redis = await PartyTest.RedisAsync();
        var directory = new MyRoomDirectory(redis);
        var rooms = Rooms(redis, Guid.Empty);
        var owner = PartyTest.People(1)[0];
        using var world = World.Create();
        Entity ownerPlayer = world.Create(), visitorPlayer = world.Create();

        var first = await directory.RouteAsync(owner, Gate, Unregistered);
        var room = await OpenAsync(rooms, first.Instance, owner);

        rooms.Join(ownerPlayer, first.Instance, room, owner, 1, 0);

        var visit = await directory.RouteAsync(owner, Gate, Unregistered);

        Assert.AreEqual(first.Instance, visit.Instance, "the visitor is sent where the owner is");

        await rooms.LeftAsync(ownerPlayer);

        Assert.IsNull(rooms.Find(first.Instance), "the room closes with its last player");

        rooms.Join(visitorPlayer, visit.Instance, await OpenAsync(rooms, visit.Instance, owner), owner + 1, 2, 0);

        Assert.AreEqual(visit.Instance, (await directory.RouteAsync(owner, Gate, Unregistered)).Instance,
            "the owner who comes back is sent into the instance the visitor is in");

        var elsewhere = await Rooms(redis, Guid.Empty).ClaimAsync(Guid.NewGuid(), owner, Gate);

        Assert.AreEqual(visit.Instance, elsewhere?.Elsewhere?.Instance, "another instance is told where the room is played");
    }

    /// <summary>
    /// When the server of an instance is taken for gone, the next player is sent into a new instance; the old one, still
    /// running, finds at its next renewal that the record names the new one, gives the room up without taking the record, and
    /// tells which players to send there.
    /// </summary>
    [TestMethod]
    public async Task AnInstanceWhoseRecordNamesAnotherGivesTheRoomUp()
    {
        var redis = await PartyTest.RedisAsync();
        var directory = new MyRoomDirectory(redis);
        var server = Guid.NewGuid();
        var rooms = Rooms(redis, server);
        var owner = PartyTest.People(1)[0];
        var presence = (RedisKey)(typeof(MazeCache).FullName + ":" + server);
        using var world = World.Create();
        var player = world.Create();

        await redis.GetDatabase().HashSetAsync(presence, "Host", "maze");
        await redis.GetDatabase().KeyExpireAsync(presence, TimeSpan.FromMinutes(1));

        var first = await directory.RouteAsync(owner, Gate, (server, "maze", 100));
        var room = await OpenAsync(rooms, first.Instance, owner);

        rooms.Join(player, first.Instance, room, owner, 1, 7);

        Assert.IsNull(await rooms.KeepAsync(first.Instance), "the record names the instance while its server is there");

        await redis.GetDatabase().KeyDeleteAsync(presence);

        var next = await directory.RouteAsync(owner, Gate, Unregistered);

        Assert.AreNotEqual(first.Instance, next.Instance);
        Assert.AreEqual(next.Instance, (await rooms.KeepAsync(first.Instance))?.Instance);
        CollectionAssert.AreEqual(new[] { (player, (short)7) }, rooms.GiveUp(first.Instance).ToArray());
        Assert.IsNull(rooms.Find(first.Instance));
        Assert.AreEqual(next.Instance, (await directory.RouteAsync(owner, Gate, Unregistered)).Instance, "the new instance keeps the record");
    }

    /// <summary>
    /// A room opened for a player who is then sent back at the door, with nobody in it, ends with its instance and gives its
    /// record back.
    /// </summary>
    [TestMethod]
    public async Task ARoomNobodyGotIntoEndsWithItsInstance()
    {
        var redis = await PartyTest.RedisAsync();
        var directory = new MyRoomDirectory(redis);
        var rooms = Rooms(redis, Guid.Empty);
        var owner = PartyTest.People(1)[0];
        var world = World.Create();

        try
        {
            var player = world.Create(
                new ActorComponent(owner, ActorType.User),
                new WorldComponent { Location = 0, Position = Vector3.Zero, Rotation = 0f, Jump = 0 });
            var instances = new MazeInstances(world, null!, NullLogger<MazeInstances>.Instance, rooms);
            var first = await directory.RouteAsync(owner, Gate, Unregistered);
            var maze = (await instances.GetOrOpenAsync(first.Instance,
                () => ValueTask.FromResult<LuaMaze?>(TestMazes.Create(world, player, instance: first.Instance))))!;

            world.Add(player, maze);

            await OpenAsync(rooms, first.Instance, owner);

            await instances.LeaveAsync(player);

            Assert.IsNull(rooms.Find(first.Instance));
            Assert.AreNotEqual(first.Instance, (await directory.RouteAsync(owner, Gate, Unregistered)).Instance, "the record was given back");
        }

        finally
        {
            World.Destroy(world);
        }
    }
}
