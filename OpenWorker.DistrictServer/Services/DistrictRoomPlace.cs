using Arch.Core;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Modules.MyRoom;

namespace OpenWorker.DistrictServer.Services;

/// <summary>
/// A player in a district stands in no room, may go into any, and comes back to this district when it leaves it.
/// </summary>
public sealed class DistrictRoomPlace(World world) : IMyRoomPlace
{
    public int? RoomOf(Entity player) => 0;

    public short ReturnOf(Entity player) => world.Get<WorldComponent>(player).Location;

    // The district lets the player go when its connection closes.
    public ValueTask LeftAsync(Entity player) => ValueTask.CompletedTask;
}
