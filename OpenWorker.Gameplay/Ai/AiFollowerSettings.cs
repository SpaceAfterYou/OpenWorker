namespace OpenWorker.Gameplay.Ai;

/// <summary>
/// How a monster gets away from a run of blows: each blow adds points, and when they reach <paramref name="Points"/> the
/// monster escapes with the chance it has (in ten thousand). The points start again from a random half after
/// <paramref name="ResetTime"/> seconds without a blow. Kind 2 plays the skill <paramref name="Value"/>; kinds 0 and 1 make
/// the monster hard to stagger or invincible for <paramref name="Value"/> milliseconds.
/// </summary>
public sealed record AiEscape(float Points, int Chance, float MinDistance, float MaxDistance, float ResetTime, byte Kind, int Value)
{
    /// <summary>
    /// The escape that makes the monster hard to stagger for a while.
    /// </summary>
    public const byte SuperArmorKind = 0;

    /// <summary>
    /// The escape that makes the monster invincible for a while.
    /// </summary>
    public const byte InvincibleKind = 1;

    /// <summary>
    /// The escape that plays a skill.
    /// </summary>
    public const byte SkillKind = 2;
}

/// <summary>
/// What the script of a helper says about following its owner: how far it may be before it comes back to the owner at
/// once (out of a fight and in one), the skill it plays when it is beaten down, and how it escapes blows.
/// </summary>
public sealed record AiFollowerSettings
{
    public float FarDistance { get; init; }

    public float FarBattleDistance { get; init; }

    public int RecoverySkill { get; init; }

    public AiEscape? Escape { get; init; }
}
