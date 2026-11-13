namespace OpenWorker.Gameplay.Modules.UnityPoints;

/// <summary>
/// The unity points of one player in the unity mode of its maze. A combo that ends while the mode runs gives battle points by
/// its length, a tenth of its square, which are paid into the purse of the player when its run ends.
/// </summary>
public sealed class UnityTally
{
    /// <summary>
    /// A combo is over once its last hit is this old.
    /// </summary>
    public const long ComboEndMilliseconds = 3000;

    // The last hit of the combo already counted, so a combo counts once.
    private long _counted = long.MinValue;

    /// <summary>
    /// The battle points earned and not yet paid.
    /// </summary>
    public int BattlePoints { get; private set; }

    /// <summary>
    /// The battle points a combo of that length gives.
    /// </summary>
    public static int FromCombo(int combo)
    {
        // Counted in single floats, which fall a little short: a combo of 3 gives nothing, one of 20 gives 39, one of 50 gives 250.
        return combo <= 0 ? 0 : (int)((float)(combo * 0.01f) * (float)(combo * 10f));
    }

    /// <summary>
    /// Counts the combo of the player whose last hit was at <paramref name="lastHit"/> once it is over at <paramref name="now"/>
    /// (milliseconds on one clock). A combo that was over before the mode started at <paramref name="since"/> counts nothing.
    /// True when a combo ended now: the player is told its points then, even when the combo was too short to give any.
    /// </summary>
    public bool CountCombo(int combo, long lastHit, long now, long since)
    {
        if (lastHit == long.MinValue || lastHit == _counted || lastHit > now - ComboEndMilliseconds)
        {
            return false;
        }

        _counted = lastHit;

        if (combo <= 0 || lastHit <= since - ComboEndMilliseconds)
        {
            return false;
        }

        BattlePoints += FromCombo(combo);

        return true;
    }

    /// <summary>
    /// The battle points to pay, which are then gone from the tally.
    /// </summary>
    public int TakeBattlePoints()
    {
        var points = BattlePoints;

        BattlePoints = 0;

        return points;
    }
}
