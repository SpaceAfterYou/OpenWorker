namespace OpenWorker.Lua;

/// <summary>
/// What the maze makes of something the client says its player did with a skill: fired a shot, let a chain go, dragged a trap.
/// </summary>
public enum ClaimVerdict
{
    /// <summary>
    /// It happens.
    /// </summary>
    Taken,

    /// <summary>
    /// Nothing happens, and it is no fault: the player is down or gone, the skill was cut by another, or the same fault was
    /// counted a moment ago.
    /// </summary>
    Dropped,

    /// <summary>
    /// Nothing happens, and it is a claim the client cannot make: more shots than the skill lets go, or a place out of reach. It
    /// counts against the error budget of the session.
    /// </summary>
    Refused
}
