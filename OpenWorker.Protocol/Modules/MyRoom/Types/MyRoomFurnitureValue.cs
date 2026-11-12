using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.MyRoom.Types;

/// <summary>
/// A piece of furniture placed in a room: the serial of the item it was, the item, which way it is turned and the cell of the
/// grid it stands on.
/// </summary>
[WireMessage(WireMethods.Both)]
public readonly partial record struct MyRoomFurnitureValue(long Serial, int Item, byte Rotation, int Grid);
