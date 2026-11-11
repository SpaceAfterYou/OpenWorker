namespace OpenWorker.Gameplay.Ai;

/// <summary>
/// What hurt the monster: a skill, a projectile or a trap. The kind decides how much the blow adds to the escape points.
/// </summary>
public enum AiHitKind : byte
{
    Skill,
    Projectile,
    Trap
}

/// <summary>
/// A blow the monster took.
/// </summary>
/// <param name="Attacker">The actor who dealt it, in the form the client keeps it in a packet.</param>
/// <param name="Hate">How much the blow angers the monster at the one who dealt it.</param>
/// <param name="Damage">The life it took.</param>
public readonly record struct AiHit(int Attacker, float Hate, int Damage = 0, AiHitKind Kind = AiHitKind.Skill);

/// <summary>
/// A defense the AI of a monster asks for, for a while.
/// </summary>
public enum AiDefense : byte
{
    /// <summary>
    /// Blows do not make it flinch.
    /// </summary>
    SuperArmor,

    /// <summary>
    /// Nothing hurts it.
    /// </summary>
    Invincible
}
