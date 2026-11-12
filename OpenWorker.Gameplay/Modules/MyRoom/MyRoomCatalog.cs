using System.Collections.ObjectModel;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.MyRoom;

/// <summary>
/// What the client's tables say about rooms: the rows a room is made from (the maze it is played in, how many others may be
/// in it, the level and the gold it takes) and the furniture an item is (how many may use a piece at once).
/// </summary>
public sealed class MyRoomCatalog
{
    /// <summary>
    /// The most players a piece of furniture takes at once, whatever its row says.
    /// </summary>
    public const int MostSeats = 4;

    /// <summary>
    /// The most pieces a room holds: the client reads the count of the furniture of a room in one byte.
    /// </summary>
    public const int MostPieces = byte.MaxValue;

    /// <summary>
    /// The ways a piece can face: the client turns a piece a quarter at a time.
    /// </summary>
    public const int Rotations = 4;

    /// <summary>
    /// The cells of a room are numbered by plane: the floor from zero, then each of the four walls from a million on.
    /// </summary>
    private const int Plane = 1_000_000;

    private readonly RowIndex<int, MyroomInfoRow> _rooms;
    private readonly RowIndex<int, MyroomFurnitureRow> _furniture;
    private readonly RowIndex<int, MyroomGreedRow> _grids;
    private readonly HashSet<int> _mazes;
    private readonly ItemCatalog _items;

    public MyRoomCatalog(ReadOnlyCollection<MyroomInfoRow> rooms, ReadOnlyCollection<MyroomFurnitureRow> furniture,
        ReadOnlyCollection<MyroomGreedRow> grids, ItemCatalog items)
    {
        _rooms = rooms.IndexBy(e => e.Id);
        _furniture = furniture.IndexBy(e => e.Id);
        _grids = grids.IndexBy(e => e.Id);
        _mazes = rooms.Select(e => e.Maze).ToHashSet();
        _items = items;
    }

    public bool TryGetRoom(int index, out MyroomInfoRow row) => _rooms.TryGet(index, out row);

    /// <summary>
    /// Whether the maze is the one rooms are played in.
    /// </summary>
    public bool IsRoomMaze(int maze) => _mazes.Contains(maze);

    /// <summary>
    /// The furniture the item is, when it is one.
    /// </summary>
    public bool TryGetFurniture(int item, out MyroomFurnitureRow row)
    {
        row = default;

        return _items.TryGet(item, out var itemRow) && itemRow.Furniture > 0 && _furniture.TryGet(itemRow.Furniture, out row);
    }

    /// <summary>
    /// How many players may use the piece at once (none for an item that is no furniture).
    /// </summary>
    public int SeatsOf(int item)
    {
        return TryGetFurniture(item, out var row) ? Math.Clamp(row.Seats, 0, MostSeats) : 0;
    }

    /// <summary>
    /// Whether the piece of the item may stand on the cell of the grid of a room, turned so: a piece of the floor (placement 1
    /// or 8) on the floor, a piece of the walls (2 or 9) on one of the four walls, all of it inside, as the client lays the
    /// grid of the room out; a piece that replaces a part of the room (the walls, the floor, a door or a window) on no cell.
    /// </summary>
    public bool Fits(short grid, int item, byte rotation, int cell)
    {
        if (rotation >= Rotations || cell < 0 || !TryGetFurniture(item, out var piece) || !_grids.TryGet(grid, out var room))
        {
            return false;
        }

        // A piece turned a quarter lies across.
        var (width, depth) = rotation % 2 == 0 ? (piece.Width, piece.Depth) : (piece.Depth, piece.Width);
        var (plane, index) = Math.DivRem(cell, Plane);

        return piece.Placement switch
        {
            1 or 8 => plane == 0 && Inside(index, width, depth, room.Width, room.Depth),
            2 or 9 => plane is >= 1 and <= 4 && Inside(index, width, depth, plane <= 2 ? room.Width : room.Depth, room.Height),
            _ => cell == 0 && rotation == 0
        };

        static bool Inside(int index, int width, int depth, int across, int along)
        {
            if (across <= 0 || along <= 0)
            {
                return false;
            }

            var (row, column) = Math.DivRem(index, across);

            return width >= 1 && depth >= 1 && column + width <= across && row + depth <= along;
        }
    }
}
