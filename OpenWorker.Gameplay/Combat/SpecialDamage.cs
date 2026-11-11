namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// The monster in a hit, as far as the special effects care: how strong a kind it is, which faction and how it fights.
/// </summary>
/// <param name="Rank">0 to 2 and 6 and 7 are plain monsters, 3 to 5 are named ones, bosses and raid bosses.</param>
/// <param name="Faction">Which side of the world the monster belongs to; the effects of a faction are for the factions from 6 on.</param>
/// <param name="StatusType">2 for a monster that shoots, anything else for one that fights close by.</param>
public readonly record struct Opponent(byte Rank, byte Faction, byte StatusType)
{
    public bool IsBoss => Rank is 3 or 4 or 5;

    public bool IsRanged => StatusType == 2;
}

/// <summary>
/// What a hit needs to know about the monster in it, and in which way it goes.
/// </summary>
public readonly record struct DamageContext
{
    /// <summary>
    /// A player hits a monster: the damage effects of the player count, by the kind of the monster.
    /// </summary>
    public static DamageContext PlayerHits(Opponent monster, bool flying, bool down) => new() { Monster = monster, MonsterIsTarget = true, Flying = flying, Down = down };

    /// <summary>
    /// A monster hits a player: the effects of the player that take damage away count, by the kind of the monster.
    /// </summary>
    public static DamageContext MonsterHits(Opponent monster) => new() { Monster = monster };

    public Opponent? Monster { get; init; }

    public bool MonsterIsTarget { get; init; }

    public bool Flying { get; init; }

    public bool Down { get; init; }
}

/// <summary>
/// The special effects of gear that raise the damage to a kind of monster, and those that lower the damage a kind of monster
/// does, applied in the order of the original: each takes its share of what the one before it left.
/// </summary>
public static class SpecialDamage
{
    private const float Percent = 0.01f;

    /// <param name="damage">The damage after the defense of the target.</param>
    /// <param name="grazed">The blow missed and only grazed.</param>
    public static int Apply(int damage, in CombatStats attacker, in CombatStats target, in DamageContext context, bool grazed)
    {
        if (context.Monster is not { } monster)
        {
            return damage;
        }

        if (context.MonsterIsTarget)
        {
            var effects = attacker.Special;

            damage = Add(damage, effects.Of(monster.IsBoss ? SpecialEffect.DamageToBoss : SpecialEffect.DamageToPlain));
            damage = Add(damage, effects.Of(monster.IsRanged ? SpecialEffect.DamageToRanged : SpecialEffect.DamageToMelee));

            if (context.Flying)
            {
                damage = Add(damage, effects.Of(SpecialEffect.DamageToFlying));
            }

            if (context.Down)
            {
                damage = Add(damage, effects.Of(SpecialEffect.DamageToDown));
            }

            return Add(damage, effects.DamageToFaction(monster.Faction));
        }

        var guard = target.Special;

        damage = Take(damage, guard.Of(monster.IsBoss ? SpecialEffect.DamageFromBoss : SpecialEffect.DamageFromPlain));
        damage = Take(damage, guard.Of(monster.IsRanged ? SpecialEffect.DamageFromRanged : SpecialEffect.DamageFromMelee));
        damage = Take(damage, guard.DamageFromFaction(monster.Faction));

        return grazed ? Take(damage, guard.Of(SpecialEffect.GrazeDamageDown)) : damage;
    }

    private static int Add(int damage, float percent) => damage + (int)(damage * (percent * Percent));

    private static int Take(int damage, float percent) => damage - (int)(damage * (percent * Percent));
}
