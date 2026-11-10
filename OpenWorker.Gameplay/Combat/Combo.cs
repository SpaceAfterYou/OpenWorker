namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// The combo of a player: its attacks that hit, one after the other. The count goes on while each attack hits within the
/// window of the last one and starts again otherwise, at the targets of the attack less one, so the first attack of a run
/// counts none. One attack counts at most five targets. The client shows the count it is sent with a hit (06/13).
/// </summary>
public static class Combo
{
    /// <summary>
    /// An attack that hits this long or longer after the last one starts the combo again.
    /// </summary>
    public const long WindowMilliseconds = 3000;

    /// <summary>
    /// The most targets one attack counts.
    /// </summary>
    public const int MaxHits = 5;

    /// <summary>
    /// The count after an attack that hit <paramref name="targets"/> targets at <paramref name="now"/>, after a combo of
    /// <paramref name="count"/> whose last hit was at <paramref name="lastHit"/> (milliseconds on one clock).
    /// </summary>
    public static int Next(int count, long lastHit, int targets, long now)
    {
        var hits = Math.Clamp(targets, 0, MaxHits);

        // Compared without taking the last hit from now, so a run that never hit (long.MinValue) does not overflow.
        return lastHit > now - WindowMilliseconds ? count + hits : Math.Max(0, hits - 1);
    }
}
