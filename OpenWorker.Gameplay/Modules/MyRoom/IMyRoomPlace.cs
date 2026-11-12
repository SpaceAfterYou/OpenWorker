using Arch.Core;

namespace OpenWorker.Gameplay.Modules.MyRoom;

/// <summary>
/// Where a player stands, as far as rooms are concerned, on the server it plays on: a district, a room, or a maze that is no
/// room. The district and the maze server each register their own.
/// </summary>
public interface IMyRoomPlace
{
    /// <summary>
    /// The account whose room the player stands in; zero in a district; nothing in a maze that is no room, from which no room
    /// can be entered.
    /// </summary>
    int? RoomOf(Entity player);

    /// <summary>
    /// The district the player goes back to when it leaves the room it is sent into from where it stands.
    /// </summary>
    short ReturnOf(Entity player);

    /// <summary>
    /// The player has been sent away from where it stands.
    /// </summary>
    ValueTask LeftAsync(Entity player);
}
