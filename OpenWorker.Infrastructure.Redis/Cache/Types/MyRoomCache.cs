namespace OpenWorker.Hotspot.Cache.Types;

/// <summary>
/// The record of the room of an account while it is played (<see cref="MyRoomDirectory"/>): the instance of the maze it plays
/// in and the maze server that runs it, how many others than the owner are in it and whether the owner arranges, as that
/// server last told.
/// </summary>
/// <param name="Server">The identifier of the maze server, which names its record (<see cref="MazeCache"/>); empty for a server
/// that does not register.</param>
public sealed record MyRoomCache(Guid Instance, Guid Server, string Host, short Port, bool Arranging, int Visitors);
