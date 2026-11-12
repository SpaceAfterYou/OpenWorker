using System.Collections.ObjectModel;
using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Modules.MyRoom;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.MyRoom.Requests;
using OpenWorker.MazeServer.Services;
using OpenWorker.UpdateContent.Res.Rows;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// What the owner does in its room on the maze server (<see cref="MyRoomService"/>), with the store of the rooms in memory.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MyRoomServiceTest
{
    private const int Account = 7;
    private const int Bed = 880000001;
    private const int FurnitureKind = 9001;

    /// <summary>
    /// A store of rooms in memory whose placing answers what the test says.
    /// </summary>
    private sealed class Store : IMyRoomStore
    {
        public Func<MyRoomPiece, Task<bool>> Place { get; set; } = _ => Task.FromResult(true);

        public List<MyRoomPiece> Placed { get; } = [];

        public Task<MyRoomRecord?> FindAsync(int account, short gate, CancellationToken cancellationToken = default) =>
            Task.FromResult<MyRoomRecord?>(null);

        public Task<IReadOnlyList<MyRoomPiece>> FurnitureAsync(int account, short gate, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MyRoomPiece>>(Placed);

        public Task<int?> AccountOfAsync(int person, short gate, CancellationToken cancellationToken = default) => Task.FromResult<int?>(null);

        public Task<bool> CreateAsync(MyRoomRecord room, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task SetupAsync(int account, short gate, byte openLevel, string name, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public async Task<bool> PlaceAsync(int account, short gate, MyRoomPiece piece, CancellationToken cancellationToken = default)
        {
            if (!await Place(piece))
            {
                return false;
            }

            Placed.Add(piece);

            return true;
        }

        public Task<bool> RemoveAsync(int account, short gate, long serial, CancellationToken cancellationToken = default) =>
            Task.FromResult(Placed.RemoveAll(e => e.Serial == serial) > 0);

        public Task MoveAsync(int account, short gate, IReadOnlyCollection<MyRoomPiece> pieces, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class Harness : IDisposable
    {
        public Harness()
        {
            var items = new ItemCatalog(
                new ReadOnlyCollection<ItemRow>([TestRows.Build<ItemRow>(("Id", Bed), ("Classify", FurnitureKind), ("Furniture", 1), ("Stack", (short)1))]),
                new ReadOnlyCollection<ItemClassifyRow>([TestRows.Build<ItemClassifyRow>(("Id", FurnitureKind), ("StorageKind", (byte)9))]),
                new([]),
                new([]));

            Catalog = new MyRoomCatalog(
                new ReadOnlyCollection<MyroomInfoRow>([TestRows.Build<MyroomInfoRow>(("Id", 1), ("Maze", 20011), ("MaxVisitors", 19))]),
                new ReadOnlyCollection<MyroomFurnitureRow>([TestRows.Build<MyroomFurnitureRow>(("Id", 1), ("ItemId", Bed), ("Seats", 1))]),
                items);

            Inventory = new InventoryService(World, items, new LocalItemSerials());

            var person = new PersonPersistent
            {
                Id = 1, Name = "Owner", Hero = Hero.Lily,
                DefaultHairStyle = 0, DefaultHairColor = 0, DefaultEyeColor = 0, DefaultSkinColor = 0,
                EquippedHairStyle = 0, EquippedHairColor = 0, EquippedEyeColor = 0, EquippedSkinColor = 0,
                Location = 0, PositionX = 0, PositionY = 0, PositionZ = 0, RotationX = 0, FatiguePointCommon = 0,
                Account = null!, Gate = null!
            };

            Player = World.Create(
                new ActorComponent(1, ActorType.User),
                new ServerSessionComponent(new RecordingConnection()),
                new WorldComponent { Location = 20011, Position = Vector3.Zero, Rotation = 0f, Jump = 0 },
                new ClaimsComponent(Account, 0),
                new InventoryComponent(new InventoryFactory(items, new([]), new LocalItemSerials()).CreateNew(person)));

            World.Add(Player, TestMazes.Create(World, Player, instance: Instance));

            Rooms = new MyRooms(
                new MyRoomDirectory(null!),
                Options.Create(new InstanceOptions { Identifier = Guid.NewGuid(), PublicHost = "maze", PublicPort = 100 }),
                Options.Create(new MazeEndpointOptions { Host = "maze", Port = 100 }),
                NullLogger<MyRooms>.Instance);

            Room = new MyRoomState(new MyRoomRecord(Account, 1, 1, 1, 0, "Room"), 19, [], Catalog);

            Rooms.Open(Instance, Room, Guid.NewGuid());
            Rooms.Join(Player, Instance, Room, Account, 1, 0);

            var book = new MyRoomBook(World, StoreOfRooms, Catalog, Inventory, Options.Create(new GateOptions { Id = 1 }), NullLogger<MyRoomBook>.Instance);

            Service = new MyRoomService(World, Rooms, book, Catalog, StoreOfRooms, null!, null!, Inventory, NoPersonFlush.Instance,
                NullLogger<MyRoomService>.Instance);
        }

        public World World { get; } = World.Create();

        public Guid Instance { get; } = Guid.NewGuid();

        public Entity Player { get; }

        public MyRoomCatalog Catalog { get; }

        public InventoryService Inventory { get; }

        public Store StoreOfRooms { get; } = new();

        public MyRooms Rooms { get; }

        public MyRoomState Room { get; }

        public MyRoomService Service { get; }

        public ServiceHandleContext Context => new(Player, CancellationToken.None);

        public ItemInstance? BedInBag() =>
            Inventory.InventoryOf(Player)?.Find(StorageGroup.Cube)?.Slots.FirstOrDefault(e => e is not null && e.Item == Bed);

        public static MyRoomItemAddRequest Place(long serial, int grid = 0, byte rotation = 0)
        {
            Span<byte> bytes = stackalloc byte[17];

            var writer = new PacketWriter(bytes);

            writer.WriteInt64(serial);
            writer.WriteInt32(Bed);
            writer.WriteByte(rotation);
            writer.WriteInt32(grid);

            var reader = new PacketReader(bytes);

            return MyRoomItemAddRequest.Read(ref reader);
        }

        public void Dispose() => World.Destroy(World);
    }

    /// <summary>
    /// The database takes a piece before its item leaves the bag: a piece it refuses, or a store that fails, leaves the item in
    /// the bag and the room as it was.
    /// </summary>
    [TestMethod]
    public async Task APieceTheDatabaseDoesNotTakeStaysInTheBag()
    {
        using var harness = new Harness();

        Assert.IsTrue(harness.Inventory.TryGive(harness.Player, Bed, 1));
        Assert.IsTrue(harness.Room.StartArranging());

        var serial = harness.BedInBag()!.Serial;

        harness.StoreOfRooms.Place = _ => Task.FromResult(false);

        await harness.Service.OnHandleAsync(harness.Context, Harness.Place(serial));

        Assert.IsNotNull(harness.BedInBag(), "a piece the database refuses stays in the bag");
        Assert.IsEmpty(harness.Room.Furniture);

        harness.StoreOfRooms.Place = _ => Task.FromException<bool>(new TimeoutException("the database does not answer"));

        await Assert.ThrowsExactlyAsync<TimeoutException>(async () => await harness.Service.OnHandleAsync(harness.Context, Harness.Place(serial)));

        Assert.IsNotNull(harness.BedInBag(), "a store that fails takes nothing");
        Assert.IsEmpty(harness.Room.Furniture);

        harness.StoreOfRooms.Place = _ => Task.FromResult(true);

        await harness.Service.OnHandleAsync(harness.Context, Harness.Place(serial));

        Assert.IsNull(harness.BedInBag(), "a piece the database took leaves the bag");
        Assert.AreEqual(serial, harness.Room.Furniture.Single().Serial);
    }
}
