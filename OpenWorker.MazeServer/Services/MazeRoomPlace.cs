using Arch.Core;
using OpenWorker.Gameplay.Modules.MyRoom;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// A player on the maze server stands in a room or in a maze that is no room. From a room it may go into another, and goes back
/// to the district it came from before it entered the first one.
/// </summary>
public sealed class MazeRoomPlace(MyRooms rooms, MazeInstances instances) : IMyRoomPlace
{
    public int? RoomOf(Entity player) => rooms.TryGet(player, out var room, out _) ? room.Room.Account : null;

    public short ReturnOf(Entity player) => rooms.TryGet(player, out _, out var back) ? back : (short)0;

    public ValueTask LeftAsync(Entity player) => instances.LeaveAsync(player);
}
