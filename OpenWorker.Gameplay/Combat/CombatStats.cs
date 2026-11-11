using OpenWorker.Domain.Enums;
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
/// How much of a state a character shrugs off: the share taken from the chance that a buff of that state sticks, out of 10000.
/// </summary>
public readonly record struct Resistances
{
    public float Burn { get; init; }
    public float Poison { get; init; }
    public float Shock { get; init; }
    public float Bleed { get; init; }
    public float Stun { get; init; }
    public float Paralysis { get; init; }
    public float Sleep { get; init; }
    public float Freeze { get; init; }
    public float Charm { get; init; }
    public float Confusion { get; init; }
    public float Silence { get; init; }
    public float Weakness { get; init; }
}

/// <summary>
/// The six attributes of damage (light, darkness, cool, abhor, heal, pain): an attack of one of them is held back by the
/// resistance of the target to it.
/// </summary>
public readonly record struct AttributeStats
{
    public float Light { get; init; }
    public float Darkness { get; init; }
    public float Cool { get; init; }
    public float Abhor { get; init; }
    public float Heal { get; init; }
    public float Pain { get; init; }

    public const int Count = 6;

    public float this[int index] => index switch
    {
        0 => Light,
        1 => Darkness,
        2 => Cool,
        3 => Abhor,
        4 => Heal,
        _ => Pain
    };
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
    /// How much soul gauge the character holds. Only the server's own status table says it; zero without it.
    /// </summary>
    public float SoulGaugeMax { get; init; }

    /// <summary>
    /// What a second adds to a full soul gauge, by the rule of the weapon of the hero.
    /// </summary>
    public float SoulGaugeRegen { get; init; }

    /// <summary>
    /// The soul gauge regeneration of the status, which the rule of the weapon multiplies.
    /// </summary>
    public float SoulGaugeConstant { get; init; }

    /// <summary>
    /// The share of the absorption of a skill that a hit on this character gives to its attacker.
    /// </summary>
    public float SoulGaugeAbsorbRate { get; init; }

    /// <summary>
    /// What gear, titles and buffs add to what a second gives the soul gauge.
    /// </summary>
    public float SoulGaugeRegenAdded { get; init; }

    /// <summary>
    /// The share in percent of what the rule of the weapon gives that gear, titles and buffs add to it.
    /// </summary>
    public float SoulGaugeRegenShare { get; init; }

    public float StaminaMax { get; init; }

    private readonly SpecialEffects? _special;

    /// <summary>
    /// The special effects of gear, titles and buffs: shares of damage to a kind of monster, of cooldown, of cost.
    /// </summary>
    public SpecialEffects Special
    {
        get => _special ?? SpecialEffects.None;
        init => _special = value.IsNone ? null : value;
    }

    /// <summary>
    /// What a second adds to the stamina.
    /// </summary>
    public float StaminaRegen { get; init; }

    /// <summary>
    /// How fast the character moves, in percent of the speed its hero has in the table: 100 is the speed of the table.
    /// </summary>
    public float MoveSpeed { get; init; }

    /// <summary>
    /// How fast the character attacks, in percent of the speed of its animations: 100 is the speed of the table.
    /// </summary>
    public float AttackSpeed { get; init; }

    /// <summary>
    /// What the character resists of the states that buffs put on it.
    /// </summary>
    public Resistances Resist { get; init; }

    /// <summary>
    /// The attack of the character in each attribute of damage.
    /// </summary>
    public AttributeStats AttributeAttack { get; init; }

    /// <summary>
    /// The resistance of the character to each attribute of damage.
    /// </summary>
    public AttributeStats AttributeResist { get; init; }

    /// <summary>
    /// What a second adds to the soul gauge of the hero at its fill: the rule of its weapon, with what is added to it.
    /// </summary>
    public float SoulGaugeRegenOf(Hero hero, float maximum, float current)
    {
        var regeneration = SoulGauge.RegenerationPerSecond(hero, Level, maximum, current, SoulGaugeConstant);

        return regeneration + SoulGaugeRegenAdded + regeneration * SoulGaugeRegenShare * 0.01f;
    }

    /// <summary>
    /// What a second adds to the soul gauge of the hero when it is full.
    /// </summary>
    public float SoulGaugeRegenOf(Hero hero, float maximum) => SoulGaugeRegenOf(hero, maximum, maximum);

    /// <summary>
    /// The stats as the client stores them. Zero values are left out, that is what the client expects them to be.
    /// </summary>
    public StatKeyValuePair[] ToStatList()
    {
        return Values()
            .Where(e => e.Value > 0)
            .Select(e => new StatKeyValuePair { byIndex = e.Type, statValue = e.Value })
            .ToArray();
    }

    /// <summary>
    /// Every stat the client stores, by its number, zero or not.
    /// </summary>
    public (STAT_TYPE Type, float Value)[] Values()
    {
        return
        [
            (STAT_TYPE.STAT_STR, Strength),
            (STAT_TYPE.STAT_AGI, Agility),
            (STAT_TYPE.STAT_INT, Intelligence),
            (STAT_TYPE.STAT_BAL, Balance),
            (STAT_TYPE.STAT_VIT, Vitality),
            (STAT_TYPE.STAT_LUC, Luck),
            (STAT_TYPE.STAT_HP_MAX, MaxHealth),
            (STAT_TYPE.STAT_SG_MAX, SoulGaugeMax),
            (STAT_TYPE.STAT_SG_REG, SoulGaugeRegen),
            (STAT_TYPE.STAT_ST_MAX, StaminaMax),
            (STAT_TYPE.STAT_ST_REG, StaminaRegen),
            (STAT_TYPE.STAT_MSR, MoveSpeed),
            (STAT_TYPE.STAT_ASR, AttackSpeed),
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
            (STAT_TYPE.STAT_MARP, Magical.Evasion),
            (STAT_TYPE.STAT_RES_BURN, Resist.Burn),
            (STAT_TYPE.STAT_RES_POISON, Resist.Poison),
            (STAT_TYPE.STAT_RES_SHOCK, Resist.Shock),
            (STAT_TYPE.STAT_RES_BLEED, Resist.Bleed),
            (STAT_TYPE.STAT_RES_STUN, Resist.Stun),
            (STAT_TYPE.STAT_RES_PARALYSIS, Resist.Paralysis),
            (STAT_TYPE.STAT_RES_SLEEP, Resist.Sleep),
            (STAT_TYPE.STAT_RES_FREEZE, Resist.Freeze),
            (STAT_TYPE.STAT_RES_CHARM, Resist.Charm),
            (STAT_TYPE.STAT_RES_CONFUSION, Resist.Confusion),
            (STAT_TYPE.STAT_RES_SILENCE, Resist.Silence),
            (STAT_TYPE.STAT_RES_WEAKNESS, Resist.Weakness)
        ];
    }

    /// <summary>
    /// The stats that differ between two states of a character, with their new values.
    /// </summary>
    public static List<(STAT_TYPE Type, float Value)> Differences(in CombatStats before, in CombatStats after)
    {
        var old = before.Values();
        var now = after.Values();
        var changed = new List<(STAT_TYPE Type, float Value)>();

        for (var index = 0; index < now.Length; index++)
        {
            if (old[index].Value != now[index].Value)
            {
                changed.Add(now[index]);
            }
        }

        return changed;
    }
}
