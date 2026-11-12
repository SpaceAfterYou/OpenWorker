using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.MyRoom.Types;

/// <summary>
/// The room of an account as the client keeps it: whose it is, which row of the room table it was made from, its grid,
/// the maze it is played in, whether others may come in (0 open, 1 closed) and its name. All zero means no room.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial record struct MyRoomOwnerValue
{
    public int Owner { get; init; }

    public short Index { get; init; }

    public short Grid { get; init; }

    public int Maze { get; init; }

    public byte OpenLevel { get; init; }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string Name { get; init; }
}
