namespace OpenWorker.Channel;

/// <summary>
/// What <see cref="MovementPace.Check"/> says of a move.
/// </summary>
public enum MoveVerdict
{
    /// <summary>
    /// The place is taken.
    /// </summary>
    Taken,

    /// <summary>
    /// The move takes the player too far in one message: the place stays and the player is warped back.
    /// </summary>
    Refused,

    /// <summary>
    /// The move is from the place the client left before it took the place the server told it: it is dropped, with no fault.
    /// </summary>
    Stale
}
