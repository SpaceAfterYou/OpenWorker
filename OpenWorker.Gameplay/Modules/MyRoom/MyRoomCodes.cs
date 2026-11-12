namespace OpenWorker.Gameplay.Modules.MyRoom;

/// <summary>
/// The codes the client shows for a room (its message table).
/// </summary>
public static class MyRoomCodes
{
    /// <summary>
    /// The level is below the one the room asks for.
    /// </summary>
    public const ushort LevelTooLow = 58200;

    /// <summary>
    /// Not enough gold to make the room.
    /// </summary>
    public const ushort NotEnoughGold = 58201;

    public const ushort AlreadyMade = 58202;

    public const ushort CreateFailed = 58203;

    public const ushort EnterFailed = 58204;

    /// <summary>
    /// The room is being arranged, so its visitors leave (and nobody comes in).
    /// </summary>
    public const ushort Arranging = 58205;

    public const ushort Full = 58206;

    /// <summary>
    /// The owner closed the room to others.
    /// </summary>
    public const ushort Closed = 58207;

    /// <summary>
    /// The character asked for has no room.
    /// </summary>
    public const ushort NoRoom = 58208;

    public const ushort ServerUnavailable = 58209;

    public const ushort KickedOut = 58211;

    /// <summary>
    /// The furniture bag has no free slot for a piece taken back.
    /// </summary>
    public const ushort BagFull = 58212;

    /// <summary>
    /// The piece has no free place for one more player.
    /// </summary>
    public const ushort NoSeat = 29064;

    /// <summary>
    /// The player stands in the room asked for already (the client has no text for it).
    /// </summary>
    public const ushort AlreadyThere = 58247;

    /// <summary>
    /// A room cannot be entered from where the player stands (the client has no text for it).
    /// </summary>
    public const ushort WrongPlace = 58249;
}
