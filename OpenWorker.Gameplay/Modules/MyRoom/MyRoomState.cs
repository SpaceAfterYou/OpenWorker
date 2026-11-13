using System.Numerics;
using Arch.Core;
using OpenWorker.Domain.Stores;

namespace OpenWorker.Gameplay.Modules.MyRoom;

/// <summary>
/// A player who uses a piece of furniture: the place of the piece it took, the animation it plays, and where it stood before
/// it sat down, which is where it stands again when it gets up.
/// </summary>
public readonly record struct MyRoomSeat(int Actor, long Serial, byte Index, byte Animation, Vector3 Position, float Rotation);

/// <summary>
/// A room while somebody is in it: its record, its furniture, who is in it, who sits where, and whether the owner is
/// arranging it. Everything runs on the world loop; what is kept is written by the caller.
/// </summary>
public sealed class MyRoomState
{
    private readonly MyRoomCatalog _catalog;
    private readonly List<MyRoomPiece> _furniture;
    private readonly Dictionary<Entity, int> _members = [];
    private readonly List<MyRoomSeat> _seats = [];
    private readonly MyRoomFrame? _frame;

    /// <param name="frame">Where the grid of the room lies in its map; with none, a player who sits stays where it stands.</param>
    public MyRoomState(MyRoomRecord room, int maxVisitors, IEnumerable<MyRoomPiece> furniture, MyRoomCatalog catalog, MyRoomFrame? frame = null)
    {
        Room = room;
        MaxVisitors = maxVisitors;
        _furniture = furniture.ToList();
        _catalog = catalog;
        _frame = frame;
    }

    public MyRoomRecord Room { get; private set; }

    /// <summary>
    /// How many others than the owner may be in the room at once.
    /// </summary>
    public int MaxVisitors { get; }

    /// <summary>
    /// The owner arranges the furniture: nobody else may be in the room meanwhile, and nobody sits.
    /// </summary>
    public bool Arranging { get; private set; }

    public IReadOnlyList<MyRoomPiece> Furniture => _furniture;

    public IReadOnlyList<MyRoomSeat> Seats => _seats;

    public IEnumerable<Entity> Members => _members.Keys;

    /// <summary>
    /// The players in the room who are not of the owner's account.
    /// </summary>
    public IEnumerable<Entity> Visitors => _members.Where(e => e.Value != Room.Account).Select(e => e.Key);

    public int VisitorCount => _members.Count(e => e.Value != Room.Account);

    public bool IsOwner(int account) => account == Room.Account;

    /// <summary>
    /// Why a player of the account may not come in, or nothing when it may. The owner always may.
    /// </summary>
    public ushort? Admit(int account)
    {
        if (IsOwner(account))
        {
            return null;
        }

        if (Arranging)
        {
            return MyRoomCodes.Arranging;
        }

        if (VisitorCount >= MaxVisitors)
        {
            return MyRoomCodes.Full;
        }

        return Room.OpenLevel != 0 ? MyRoomCodes.Closed : null;
    }

    public void Join(Entity player, int account) => _members[player] = account;

    /// <summary>
    /// A player leaves the room; the place it sat on is free again. True when nobody is left.
    /// </summary>
    public bool Leave(Entity player, int actor)
    {
        _members.Remove(player);
        _seats.RemoveAll(e => e.Actor == actor);

        return _members.Count == 0;
    }

    public bool Contains(Entity player) => _members.ContainsKey(player);

    /// <summary>
    /// The owner starts arranging. False when it is arranging already. Nobody sits while the furniture moves: every place is
    /// free again (the caller stands the owner up and sends the others out).
    /// </summary>
    public bool StartArranging()
    {
        if (Arranging)
        {
            return false;
        }

        Arranging = true;
        _seats.Clear();

        return true;
    }

    public void StopArranging() => Arranging = false;

    public void Setup(byte openLevel, string name) => Room = Room with { OpenLevel = openLevel, Name = name };

    public bool Has(long serial) => _furniture.Exists(e => e.Serial == serial);

    /// <summary>
    /// Whether the piece may be placed now: while the owner arranges, a serial the room does not have, on a cell of the grid
    /// of the room it fits, and while the room has fewer pieces than the client can be told of.
    /// </summary>
    public bool CanPlace(MyRoomPiece piece)
    {
        return Arranging && !Has(piece.Serial) && _furniture.Count < MyRoomCatalog.MostPieces &&
               _catalog.Fits(Room.Grid, piece.Item, piece.Rotation, piece.Grid);
    }

    /// <summary>
    /// Places a piece while the owner arranges. False when it may not be placed (<see cref="CanPlace"/>).
    /// </summary>
    public bool Place(MyRoomPiece piece)
    {
        if (!CanPlace(piece))
        {
            return false;
        }

        _furniture.Add(piece);

        return true;
    }

    /// <summary>
    /// Takes a piece away while the owner arranges.
    /// </summary>
    public bool TryRemove(long serial, out MyRoomPiece piece)
    {
        var index = Arranging ? _furniture.FindIndex(e => e.Serial == serial) : -1;

        if (index < 0)
        {
            piece = default;
            return false;
        }

        piece = _furniture[index];
        _furniture.RemoveAt(index);
        _seats.RemoveAll(e => e.Serial == serial);

        return true;
    }

    /// <summary>
    /// Moves and turns the pieces of the room that are named, and tells which ones it moved; a serial the room does not have,
    /// or a cell or a turn the piece does not fit, is passed over.
    /// </summary>
    public List<MyRoomPiece> Move(IEnumerable<MyRoomPiece> pieces)
    {
        var moved = new List<MyRoomPiece>();

        foreach (var piece in pieces)
        {
            var index = _furniture.FindIndex(e => e.Serial == piece.Serial);

            if (index < 0 || !_catalog.Fits(Room.Grid, _furniture[index].Item, piece.Rotation, piece.Grid))
            {
                continue;
            }

            // The item of a piece never changes: only where it stands and which way it faces.
            var placed = _furniture[index] with { Rotation = piece.Rotation, Grid = piece.Grid };

            _furniture[index] = placed;
            moved.Add(placed);
        }

        return moved;
    }

    /// <summary>
    /// A player sits or lies on a piece: one of the free places of it, at random, as many as the furniture table gives it. Null
    /// while the room is arranged, for a piece the room does not have or one that is full; the player then keeps the place it
    /// has. A player who sits elsewhere leaves that place for the new one, and still gets up where it stood before it first sat.
    /// </summary>
    public MyRoomSeat? Sit(int actor, long serial, byte animation, Vector3 position, float rotation, Random random)
    {
        if (Arranging || _furniture.Find(e => e.Serial == serial) is not { Serial: not 0 } piece)
        {
            return null;
        }

        var seats = _catalog.SeatsOf(piece.Item);

        // The place the player has on this piece is free for it.
        var taken = _seats.Where(e => e.Serial == serial && e.Actor != actor).Select(e => (int)e.Index).ToHashSet();
        var free = Enumerable.Range(0, seats).Where(e => !taken.Contains(e)).ToArray();

        if (free.Length == 0)
        {
            return null;
        }

        if (_seats.FindIndex(e => e.Actor == actor) is >= 0 and var before)
        {
            (position, rotation) = (_seats[before].Position, _seats[before].Rotation);
            _seats.RemoveAt(before);
        }

        var seat = new MyRoomSeat(actor, serial, (byte)free[random.Next(free.Length)], animation, position, rotation);

        _seats.Add(seat);

        return seat;
    }

    /// <summary>
    /// Where a player who sits on the piece is put: the middle of the cells it covers. Nothing for a piece the room does not
    /// have, or when the room does not know where its grid lies.
    /// </summary>
    public Vector3? PlaceOf(long serial)
    {
        return _frame is { } frame && _furniture.Find(e => e.Serial == serial) is { Serial: not 0 } piece
            ? _catalog.PlaceOf(Room.Grid, piece, frame)
            : null;
    }

    /// <summary>
    /// A player gets up from the piece it sits on. Null when it sits nowhere.
    /// </summary>
    public MyRoomSeat? Stand(int actor)
    {
        var index = _seats.FindIndex(e => e.Actor == actor);

        if (index < 0)
        {
            return null;
        }

        var seat = _seats[index];
        _seats.RemoveAt(index);

        return seat;
    }
}
