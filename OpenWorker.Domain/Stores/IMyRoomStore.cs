namespace OpenWorker.Domain.Stores;

/// <summary>
/// The room of an account on a gate.
/// </summary>
/// <param name="Index">The row of the room table it was made from.</param>
/// <param name="OpenLevel">0 when others may come in, 1 when it is closed to them.</param>
public readonly record struct MyRoomRecord(int Account, short Gate, short Index, short Grid, byte OpenLevel, string Name);

/// <summary>
/// A piece of furniture placed in a room, under the serial of the item it was.
/// </summary>
public readonly record struct MyRoomPiece(long Serial, int Item, byte Rotation, int Grid);

/// <summary>
/// The rooms of the accounts and their furniture. Each answer is what the rows hold now; a room is made once per account and
/// gate, and a serial is placed once, by the keys of the tables, so two servers that try at once do it once.
/// </summary>
public interface IMyRoomStore
{
    Task<MyRoomRecord?> FindAsync(int account, short gate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MyRoomPiece>> FurnitureAsync(int account, short gate, CancellationToken cancellationToken = default);

    /// <summary>
    /// The account a character of the gate belongs to, or nothing for a character the gate does not have.
    /// </summary>
    Task<int?> AccountOfAsync(int person, short gate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes the room. False when the account has one already.
    /// </summary>
    Task<bool> CreateAsync(MyRoomRecord room, CancellationToken cancellationToken = default);

    Task SetupAsync(int account, short gate, byte openLevel, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Places a piece. False when the serial stands in a room already.
    /// </summary>
    Task<bool> PlaceAsync(int account, short gate, MyRoomPiece piece, CancellationToken cancellationToken = default);

    /// <summary>
    /// Takes a piece out of the room. False when it was not there.
    /// </summary>
    Task<bool> RemoveAsync(int account, short gate, long serial, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves and turns the pieces of the room that are named; a serial the room does not have is passed over.
    /// </summary>
    Task MoveAsync(int account, short gate, IReadOnlyCollection<MyRoomPiece> pieces, CancellationToken cancellationToken = default);
}
