namespace OpenWorker.Domain.Persistent;

/// <summary>
/// The room of an account on a gate, one at most (the key): the row of the room table it was made from, its grid, its name and
/// whether others may come in. Every character of the account on the gate owns it.
/// </summary>
public sealed class MyRoomPersistent : BasicPersistent
{
    public int AccountId { get; init; }

    public short GateId { get; init; }

    /// <summary>
    /// The row of the room table the room was made from.
    /// </summary>
    public short Index { get; init; }

    public short Grid { get; init; }

    /// <summary>
    /// 0 when others may come in, 1 when the room is closed to them.
    /// </summary>
    public byte OpenLevel { get; set; }

    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// A piece of furniture placed in a room: the item it was, kept under the serial of that item, which way it is turned and the
/// cell it stands on. A serial stands in one room once (the key); the item is out of every bag while it stands there.
/// </summary>
public sealed class MyRoomFurniturePersistent : BasicPersistent
{
    public long Serial { get; init; }

    public int AccountId { get; init; }

    public short GateId { get; init; }

    public int ItemId { get; init; }

    public byte Rotation { get; set; }

    public int Grid { get; set; }
}
