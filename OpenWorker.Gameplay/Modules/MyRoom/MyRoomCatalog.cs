using System.Collections.ObjectModel;
using System.Numerics;
using OpenWorker.Batch.Entities.Basic;
using OpenWorker.Domain.Stores;
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

    /// <summary>
    /// The side of a cell of the grid, in units of the map.
    /// </summary>
    public const float Cell = 50;

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

    /// <summary>
    /// Where the grid of a room lies in its map: on the box the grid's row names, turned as the box is. Nothing when the map
    /// has no such box.
    /// </summary>
    public MyRoomFrame? FrameOf(short grid, IEnumerable<BasicEntity> boxes)
    {
        if (!_grids.TryGet(grid, out var room) || boxes.FirstOrDefault(e => e.Id == room.Box) is not { } box)
        {
            return null;
        }

        var middle = (box.PosTopLeft + box.PosBottomRight) / 2;

        return new MyRoomFrame(middle with { Z = MathF.Min(box.PosTopLeft.Z, box.PosBottomRight.Z) }, box.Rotation);
    }

    /// <summary>
    /// The middle of the cells a piece covers, in the map: on the floor, at the height of the floor; on a wall, against it. Nothing
    /// for a piece that stands on no cell of the grid.
    /// </summary>
    public Vector3? PlaceOf(short grid, MyRoomPiece piece, MyRoomFrame frame)
    {
        if (!Fits(grid, piece.Item, piece.Rotation, piece.Grid) || !TryGetFurniture(piece.Item, out var row) || row.Placement is not (1 or 8 or 2 or 9) ||
            !_grids.TryGet(grid, out var room))
        {
            return null;
        }

        var (width, depth) = piece.Rotation % 2 == 0 ? (row.Width, row.Depth) : (row.Depth, row.Width);
        var (plane, index) = Math.DivRem(piece.Grid, Plane);
        var halfWidth = room.Width * Cell / 2;
        var halfDepth = room.Depth * Cell / 2;

        // The middle of a run of cells along an edge of the grid that starts from its corner.
        float Across(int first, int count, float half) => -half + Cell * first + Cell * count / 2;

        var local = plane switch
        {
            0 => new Vector3(Across(index % room.Width, width, halfWidth), Across(index / room.Width, depth, halfDepth), 0),
            1 or 2 => new Vector3(Across(index % room.Width, width, halfWidth), plane == 1 ? -halfDepth : halfDepth,
                Cell * (index / room.Width) + Cell * depth / 2),
            _ => new Vector3(plane == 3 ? -halfWidth : halfWidth, Across(index % room.Depth, width, halfDepth),
                Cell * (index / room.Depth) + Cell * depth / 2)
        };

        return frame.Middle + Vector3.Transform(local, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -frame.Yaw * MathF.PI / 180));
    }
}

/// <summary>
/// Where the grid of a room lies in its map: the middle of its floor and the way it is turned, in degrees.
/// </summary>
public readonly record struct MyRoomFrame(Vector3 Middle, float Yaw);
