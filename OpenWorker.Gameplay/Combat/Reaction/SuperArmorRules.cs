namespace OpenWorker.Gameplay.Combat;

/// <param name="Gauge">What is left of the gauge of the armor after the blow.</param>
/// <param name="Broken">The blow broke the armor: the monster is open to everything until the armor comes back.</param>
public readonly record struct ArmorHit(float Gauge, bool Broken);

/// <summary>
/// The super armor of a monster: a gauge the blows lower, that shields the monster from reactions while it lasts, and that
/// is whole again some time after it broke.
/// </summary>
public static class SuperArmorRules
{
    /// <summary>
    /// The time an armor stays broken after its animations when the monster's group says nothing, in milliseconds.
    /// </summary>
    public const long DefaultRecoveryMilliseconds = 3000;

    private const float Percent = 0.01f;

    /// <summary>
    /// Blows this hard break the armor whatever is left in the gauge.
    /// </summary>
    private static bool BreaksAtOnce(int hardness) => hardness is 3 or 4;

    /// <summary>
    /// How much one hit takes from the gauge: the damage the hit deals, by the share the skill gives to the armor (a skill
    /// with none leaves the armor alone).
    /// </summary>
    public static float Damage(int hitDamage, float skillShare)
    {
        if (hitDamage <= 0 || skillShare <= 0f)
        {
            return 0f;
        }

        return (int)(hitDamage * (skillShare * Percent));
    }

    /// <summary>
    /// A blow lands on an armor that holds. The gauge falls by the damage and the armor breaks when nothing is left, or at once
    /// for a hard blow.
    /// </summary>
    public static ArmorHit Take(float gauge, float damage, int hardness)
    {
        var left = gauge - damage;

        return BreaksAtOnce(hardness) || left <= 0f ? new ArmorHit(0f, true) : new ArmorHit(left, false);
    }

    /// <summary>
    /// How long a broken armor stays broken, in seconds: the two animations of the break, which the monster may not have,
    /// and the time its group gives it to recover (the default when the group gives none).
    /// </summary>
    public static float BreakSeconds(float startLength, float endLength, long recoveryMilliseconds)
    {
        var recovery = recoveryMilliseconds > 0 ? recoveryMilliseconds : DefaultRecoveryMilliseconds;

        return startLength + endLength + (recovery / 1000f);
    }
}
