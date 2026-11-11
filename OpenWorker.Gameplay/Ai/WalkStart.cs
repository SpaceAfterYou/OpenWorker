namespace OpenWorker.Gameplay.Ai;

/// <summary>
/// How a monster that stands begins to walk.
/// </summary>
public enum WalkStarts
{
    /// <summary>
    /// It faces the way it goes, near enough: it sets off.
    /// </summary>
    Walk,

    /// <summary>
    /// It turns where it stands with the animation of a turn, and walks when the turn is over.
    /// </summary>
    Turn,

    /// <summary>
    /// The same, with the animation of a turn that takes it more than a quarter of the way round.
    /// </summary>
    BigTurn,

    /// <summary>
    /// It has no animation to turn with: it turns at once and the clients wait a moment before it walks.
    /// </summary>
    Pause
}

/// <summary>
/// A monster that has to go where it does not look turns first. The rule is the one of the original: within a quarter of a
/// turn it sets off at once.
/// </summary>
public static class WalkStart
{
    /// <summary>
    /// How far from the way it goes a monster may look and still set off at once, in degrees.
    /// </summary>
    public const float Tolerance = 45f;

    /// <summary>
    /// A turn beyond this many degrees is a big one, if the monster has an animation for it.
    /// </summary>
    public const float BigTurn = 90f;

    /// <summary>
    /// How long the clients wait before a monster without an animation of turning walks, in seconds.
    /// </summary>
    public const float PauseSeconds = 0.3f;

    /// <param name="current">The way the monster looks, in degrees.</param>
    /// <param name="wanted">The way it goes.</param>
    /// <param name="hasTurn">The monster has the animations of turning left and right.</param>
    /// <param name="hasBigTurn">The monster has the animations of a big turn.</param>
    /// <param name="left">The turn is to the left, which is the way the angle grows.</param>
    public static WalkStarts Of(float current, float wanted, bool hasTurn, bool hasBigTurn, out bool left)
    {
        var difference = (wanted - current) % 360f;

        if (difference > 180f)
        {
            difference -= 360f;
        }
        else if (difference < -180f)
        {
            difference += 360f;
        }

        left = difference > 0f;

        var size = MathF.Abs(difference);

        if (size <= Tolerance)
        {
            return WalkStarts.Walk;
        }

        if (!hasTurn)
        {
            return WalkStarts.Pause;
        }

        return size > BigTurn && hasBigTurn ? WalkStarts.BigTurn : WalkStarts.Turn;
    }
}
