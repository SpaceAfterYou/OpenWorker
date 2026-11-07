using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Hotspot.Modules.World.Responses;

namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// Everything an attack depends on that differs between physical and magical damage.
/// </summary>
public readonly record struct DamageStats
{
    public float AttackMin { get; init; }
    public float AttackMax { get; init; }
    public float Defense { get; init; }
    public float Accuracy { get; init; }
    public float Evasion { get; init; }
    public float CriticalChance { get; init; }
    public float CriticalResist { get; init; }
    public float CriticalDamage { get; init; }
    public float DamageReduction { get; init; }

    /// <summary>
    /// The part of the reduction that comes with the class and not from gear: an attacker carries it into its own hits.
    /// </summary>
    public float ClassDamageReduction { get; init; }
}

/// <summary>
/// What a character is capable of at its level.
/// </summary>
public readonly record struct CombatStats
{
    public byte Level { get; init; }
    public float Strength { get; init; }
    public float Agility { get; init; }
    public float Intelligence { get; init; }
    public float Balance { get; init; }
    public float Vitality { get; init; }
    public float Luck { get; init; }
    public float MaxHealth { get; init; }

    /// <summary>
    /// In percent, how much of a hit is left when it misses.
    /// </summary>
    public float GrazeDamage { get; init; }

    public DamageStats Physical { get; init; }
    public DamageStats Magical { get; init; }

    /// <summary>
    /// The stats as the client stores them. Zero values are left out, that is what the client expects them to be.
    /// </summary>
    public StatKeyValuePair[] ToStatList()
    {
        (STAT_TYPE Type, float Value)[] stats =
        [
            (STAT_TYPE.STAT_STR, Strength),
            (STAT_TYPE.STAT_AGI, Agility),
            (STAT_TYPE.STAT_INT, Intelligence),
            (STAT_TYPE.STAT_BAL, Balance),
            (STAT_TYPE.STAT_VIT, Vitality),
            (STAT_TYPE.STAT_LUC, Luck),
            (STAT_TYPE.STAT_HP_MAX, MaxHealth),
            (STAT_TYPE.STAT_PATK_MIN, Physical.AttackMin),
            (STAT_TYPE.STAT_PATK_MAX, Physical.AttackMax),
            (STAT_TYPE.STAT_MATK_MIN, Magical.AttackMin),
            (STAT_TYPE.STAT_MATK_MAX, Magical.AttackMax),
            (STAT_TYPE.STAT_PDEF, Physical.Defense),
            (STAT_TYPE.STAT_MDEF, Magical.Defense),
            (STAT_TYPE.STAT_PAR, Physical.Accuracy),
            (STAT_TYPE.STAT_MAR, Magical.Accuracy),
            (STAT_TYPE.STAT_ADR, GrazeDamage),
            (STAT_TYPE.STAT_PCP, Physical.CriticalChance),
            (STAT_TYPE.STAT_MCP, Magical.CriticalChance),
            (STAT_TYPE.STAT_PCRP, Physical.CriticalResist),
            (STAT_TYPE.STAT_MCRP, Magical.CriticalResist),
            (STAT_TYPE.STAT_PCA, Physical.CriticalDamage),
            (STAT_TYPE.STAT_MCA, Magical.CriticalDamage),
            (STAT_TYPE.STAT_PDSR, Physical.DamageReduction),
            (STAT_TYPE.STAT_MDSR, Magical.DamageReduction),
            (STAT_TYPE.STAT_PARP, Physical.Evasion),
            (STAT_TYPE.STAT_MARP, Magical.Evasion)
        ];

        return stats
            .Where(e => e.Value > 0)
            .Select(e => new StatKeyValuePair { byIndex = e.Type, statValue = e.Value })
            .ToArray();
    }
}
