using System.Collections.ObjectModel;
using System.Numerics;
using Arch.Core;
using OpenWorker.Domain.Stores;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.MyRoom;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The rules of a room while somebody is in it (<see cref="MyRoomState"/>): who may come in, the furniture the owner arranges,
/// and the places a piece has for players to sit or lie on.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MyRoomTest
{
    private const int Owner = 7;
    private const int Visitor = 8;
    private const int Bed = 880000001;
    private const int Sofa = 880000002;
    private const int Table = 880000003;
    private const int Picture = 880000004;
    private const int Wallpaper = 880000005;

    internal static MyRoomCatalog Catalog()
    {
        var items = new ReadOnlyCollection<ItemRow>(
        [
            TestRows.Build<ItemRow>(("Id", Bed), ("Furniture", 1)),
            TestRows.Build<ItemRow>(("Id", Sofa), ("Furniture", 2)),
            TestRows.Build<ItemRow>(("Id", Table), ("Furniture", 3)),
            TestRows.Build<ItemRow>(("Id", Picture), ("Furniture", 4)),
            TestRows.Build<ItemRow>(("Id", Wallpaper), ("Furniture", 5))
        ]);

        var furniture = new ReadOnlyCollection<MyroomFurnitureRow>(
        [
            TestRows.Build<MyroomFurnitureRow>(("Id", 1), ("ItemId", Bed), ("Placement", 1), ("Width", 3), ("Depth", 4), ("Seats", 1)),
            TestRows.Build<MyroomFurnitureRow>(("Id", 2), ("ItemId", Sofa), ("Placement", 1), ("Width", 2), ("Depth", 1), ("Seats", 9)),
            TestRows.Build<MyroomFurnitureRow>(("Id", 3), ("ItemId", Table), ("Placement", 1), ("Width", 1), ("Depth", 1), ("Seats", 0)),
            TestRows.Build<MyroomFurnitureRow>(("Id", 4), ("ItemId", Picture), ("Placement", 2), ("Width", 2), ("Depth", 3)),
            TestRows.Build<MyroomFurnitureRow>(("Id", 5), ("ItemId", Wallpaper), ("Placement", 3), ("Width", 1), ("Depth", 1))
        ]);

        // The grid of the client's room: 26 cells across, 28 deep, walls 8 high.
        var grids = new ReadOnlyCollection<MyroomGreedRow>([TestRows.Build<MyroomGreedRow>(("Id", 1), ("Width", 26), ("Depth", 28), ("Height", 8))]);

        var rooms = new ReadOnlyCollection<MyroomInfoRow>([TestRows.Build<MyroomInfoRow>(("Id", 1), ("Maze", 20011), ("MaxVisitors", 2))]);

        var catalog = new ItemCatalog(items, new ReadOnlyCollection<ItemClassifyRow>([]), new ReadOnlyCollection<InvenSlotExtendRow>([]),
            new ReadOnlyCollection<BankSlotExtendRow>([]));

        return new MyRoomCatalog(rooms, furniture, grids, catalog);
    }

    private static MyRoomState Room(byte openLevel = 0, params MyRoomPiece[] furniture)
    {
        return new MyRoomState(new MyRoomRecord(Owner, 1, 1, 1, openLevel, "Room"), maxVisitors: 2, furniture, Catalog());
    }

    [TestMethod]
    public void TheOwnerAlwaysComesInAndAVisitorOnlyIntoAnOpenRoomThatIsNotArrangedOrFull()
    {
        using var world = World.Create();

        var room = Room();

        Assert.IsNull(room.Admit(Visitor));

        room.Join(world.Create(), Visitor);
        room.Join(world.Create(), Visitor + 1);

        Assert.AreEqual(MyRoomCodes.Full, room.Admit(Visitor + 2), "the row lets two others in at once");
        Assert.IsNull(room.Admit(Owner), "the owner does not count against the visitors");

        var arranged = Room();

        arranged.StartArranging();

        Assert.AreEqual(MyRoomCodes.Arranging, arranged.Admit(Visitor));
        Assert.IsNull(arranged.Admit(Owner));

        Assert.AreEqual(MyRoomCodes.Closed, Room(openLevel: 1).Admit(Visitor));
        Assert.IsNull(Room(openLevel: 1).Admit(Owner));
    }

    [TestMethod]
    public void FurnitureIsPlacedOnlyWhileTheOwnerArrangesAndASerialStandsOnce()
    {
        var room = Room();
        var bed = new MyRoomPiece(100, Bed, 0, 12);

        Assert.IsFalse(room.Place(bed), "nothing is placed outside the arrangement");

        room.StartArranging();

        Assert.IsTrue(room.Place(bed));
        Assert.IsFalse(room.Place(bed with { Grid = 13 }), "a serial stands in the room once");

        // A move keeps the item of the piece and passes over a serial the room does not have.
        var moved = room.Move([bed with { Item = Sofa, Rotation = 2, Grid = 40 }, new MyRoomPiece(999, Bed, 1, 1)]);

        Assert.AreEqual(1, moved.Count);
        Assert.AreEqual(new MyRoomPiece(100, Bed, 2, 40), room.Furniture.Single());

        Assert.IsTrue(room.TryRemove(100, out var removed));
        Assert.AreEqual(Bed, removed.Item);
        Assert.IsFalse(room.TryRemove(100, out _));
    }

    [TestMethod]
    public void APieceTakesAsManyPlayersAsItsSeatsAndAtMostFour()
    {
        var room = Room(0, new MyRoomPiece(1, Bed, 0, 0), new MyRoomPiece(2, Sofa, 0, 0), new MyRoomPiece(3, Table, 0, 0));
        var random = new Random(5);

        Assert.IsNotNull(room.Sit(10, 1, 0, Vector3.One, 0, random));
        Assert.IsNull(room.Sit(11, 1, 0, Vector3.One, 0, random), "the bed has one place");
        Assert.IsNull(room.Sit(11, 3, 0, Vector3.One, 0, random), "nobody sits on a table");

        // The sofa's row says nine; a piece takes four at most, each on a place of its own.
        var places = Enumerable.Range(20, 5).Select(actor => room.Sit(actor, 2, 0, Vector3.One, 0, random)).ToArray();

        Assert.AreEqual(4, places.Count(e => e is not null));
        CollectionAssert.AreEquivalent(new byte[] { 0, 1, 2, 3 }, places.OfType<MyRoomSeat>().Select(e => e.Index).ToArray());

        // Getting up gives back where the player stood before; the place is free for the next one.
        var seat = room.Stand(20);

        Assert.AreEqual(Vector3.One, seat?.Position);
        Assert.IsNotNull(room.Sit(24, 2, 0, Vector3.Zero, 0, random));

        // Nobody sits while the owner arranges, on a piece with a free place either.
        room.Stand(21);
        room.StartArranging();

        Assert.IsNull(room.Sit(30, 2, 0, Vector3.Zero, 0, random));
    }

    [TestMethod]
    public void APieceStandsOnACellOfTheGridItFitsAndARoomHoldsWhatTheClientCanBeToldOf()
    {
        var room = Room();

        room.StartArranging();

        // The floor is 26 cells across and 28 deep: a bed of 3 by 4 fits up to the edge, not over it.
        Assert.IsTrue(room.CanPlace(new MyRoomPiece(1, Bed, 0, 23)));
        Assert.IsFalse(room.CanPlace(new MyRoomPiece(1, Bed, 0, 24)));
        Assert.IsTrue(room.CanPlace(new MyRoomPiece(1, Bed, 1, 22)), "turned a quarter, it lies four across");
        Assert.IsFalse(room.CanPlace(new MyRoomPiece(1, Bed, 1, 23)));
        Assert.IsFalse(room.CanPlace(new MyRoomPiece(1, Bed, 0, 26 * 25)), "four rows deep from the 26th row is past the floor");
        Assert.IsFalse(room.CanPlace(new MyRoomPiece(1, Bed, 4, 0)), "a piece turns a quarter at a time");
        Assert.IsFalse(room.CanPlace(new MyRoomPiece(1, Bed, 0, 1_000_000)), "a bed stands on the floor");

        // A picture hangs on a wall: the first two are 26 cells across, the others 28, all of them 8 high.
        Assert.IsTrue(room.CanPlace(new MyRoomPiece(2, Picture, 0, 1_000_000 + 26 * 5 + 24)));
        Assert.IsFalse(room.CanPlace(new MyRoomPiece(2, Picture, 0, 1_000_000 + 26 * 6)));
        Assert.IsTrue(room.CanPlace(new MyRoomPiece(2, Picture, 0, 3_000_000 + 26)));
        Assert.IsFalse(room.CanPlace(new MyRoomPiece(2, Picture, 0, 5_000_000)));
        Assert.IsFalse(room.CanPlace(new MyRoomPiece(2, Picture, 0, 0)), "a picture is not put on the floor");

        // A wallpaper replaces a part of the room and stands on no cell.
        Assert.IsTrue(room.CanPlace(new MyRoomPiece(3, Wallpaper, 0, 0)));
        Assert.IsFalse(room.CanPlace(new MyRoomPiece(3, Wallpaper, 0, 5)));

        // A move to where the piece does not fit leaves it where it stands.
        Assert.IsTrue(room.Place(new MyRoomPiece(10, Table, 0, 0)));
        Assert.IsEmpty(room.Move([new MyRoomPiece(10, Table, 0, 26 * 28)]));
        Assert.AreEqual(0, room.Furniture.Single().Grid);

        // The client is told the furniture of a room with a count of one byte.
        for (var cell = 1; room.Furniture.Count < MyRoomCatalog.MostPieces; cell++)
        {
            Assert.IsTrue(room.Place(new MyRoomPiece(10 + cell, Table, 0, cell)));
        }

        Assert.IsFalse(room.CanPlace(new MyRoomPiece(999, Table, 0, 700)));
    }

    [TestMethod]
    public void ARefusedSitKeepsThePlaceThePlayerHasAndNoPlaceOutlivesItsPiece()
    {
        var room = Room(0, new MyRoomPiece(1, Bed, 0, 0), new MyRoomPiece(2, Sofa, 0, 30));
        var random = new Random(5);

        var sofa = room.Sit(10, 2, 0, Vector3.One, 0, random);

        Assert.IsNotNull(room.Sit(11, 1, 0, Vector3.One, 0, random));
        Assert.IsNull(room.Sit(10, 1, 0, Vector3.Zero, 0, random), "the bed is taken");
        Assert.AreEqual(sofa, room.Seats.Single(e => e.Actor == 10), "the player who could not move keeps its place on the sofa");

        // Arranging frees every place, and a piece taken away takes no place with it.
        room.StartArranging();

        Assert.IsEmpty(room.Seats);
    }
}
