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

    private readonly RowIndex<int, MyroomInfoRow> _rooms;
    private readonly RowIndex<int, MyroomFurnitureRow> _furniture;
    private readonly HashSet<int> _mazes;
    private readonly ItemCatalog _items;

    public MyRoomCatalog(ReadOnlyCollection<MyroomInfoRow> rooms, ReadOnlyCollection<MyroomFurnitureRow> furniture, ItemCatalog items)
    {
        _rooms = rooms.IndexBy(e => e.Id);
        _furniture = furniture.IndexBy(e => e.Id);
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
}
