using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.MyRoom.Types;

/// <summary>
/// A player who uses a piece of furniture: the player, the piece, the place on it (from 0) and the animation it plays.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial record struct MyRoomSeatValue(int Actor, long Serial, byte Index, byte Animation);
