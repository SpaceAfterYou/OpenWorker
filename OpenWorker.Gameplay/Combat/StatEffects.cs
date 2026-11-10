using OpenWorker.Gameplay.Modules.Persons;
using OpenWorker.Hotspot.Modules.Persons.Responses;

namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// What an effect that comes from a title (or from gear) does to the characteristics of a character.
/// </summary>
public static class StatEffects
{
    /// <summary>
    /// Every stat effect of the tables (items, options, gems, sets, broaches, titles, buffs) and the stat it works on: a number
    /// added to the stat, or a share in percent of what the formula of the stat gives. Numbers past 93 are special effects (of
    /// damage, cooldowns, gold), not stats.
    /// </summary>
    private static readonly (STAT_TYPE Stat, bool Added)[] Map = BuildMap();

    /// <summary>
    /// The stat an effect works on and whether it adds a number (or a share in percent).
    /// </summary>
    public static bool TryMap(int effect, out STAT_TYPE stat, out bool added)
    {
        if (effect > 0 && effect < Map.Length && Map[effect].Stat != STAT_TYPE.STAT_NONE)
        {
            (stat, added) = Map[effect];

            return true;
        }

        stat = STAT_TYPE.STAT_NONE;
        added = false;

        return false;
    }

    /// <summary>
    /// The life, soul gauge, stamina and soul vapor a character has now: an effect on them gives or takes some once, it is
    /// not a stat.
    /// </summary>
    public static bool IsCurrentValue(STAT_TYPE stat)
    {
        return stat is STAT_TYPE.STAT_HP_CUR or STAT_TYPE.STAT_SG_CUR or STAT_TYPE.STAT_ST_CUR or STAT_TYPE.STAT_SV_CUR;
    }

    private static (STAT_TYPE, bool)[] BuildMap()
    {
        var map = new (STAT_TYPE, bool)[94];

        // The six main stats, life, the gauges and soul vapor come in pairs: a number, then a share.
        STAT_TYPE[] paired =
        [
            STAT_TYPE.STAT_STR, STAT_TYPE.STAT_AGI, STAT_TYPE.STAT_INT, STAT_TYPE.STAT_BAL, STAT_TYPE.STAT_VIT, STAT_TYPE.STAT_LUC,
            STAT_TYPE.STAT_HP_CUR, STAT_TYPE.STAT_HP_MAX, STAT_TYPE.STAT_SG_CUR, STAT_TYPE.STAT_SG_MAX, STAT_TYPE.STAT_ST_CUR,
            STAT_TYPE.STAT_ST_MAX, STAT_TYPE.STAT_ST_REG, STAT_TYPE.STAT_SV_CUR, STAT_TYPE.STAT_SV_MAX
        ];

        for (var i = 0; i < paired.Length; i++)
        {
            map[1 + 2 * i] = (paired[i], true);
            map[2 + 2 * i] = (paired[i], false);
        }

        map[31] = (STAT_TYPE.STAT_MSR, false);
        map[32] = (STAT_TYPE.STAT_ASR, false);
        map[33] = (STAT_TYPE.STAT_PATK_MAX, true);
        map[34] = (STAT_TYPE.STAT_PATK_MAX, false);
        map[35] = (STAT_TYPE.STAT_MATK_MAX, true);
        map[36] = (STAT_TYPE.STAT_MATK_MAX, false);
        map[37] = (STAT_TYPE.STAT_PDEF, true);
        map[38] = (STAT_TYPE.STAT_PDEF, false);
        map[39] = (STAT_TYPE.STAT_MDEF, true);
        map[40] = (STAT_TYPE.STAT_MDEF, false);
        map[41] = (STAT_TYPE.STAT_PAR, true);
        map[42] = (STAT_TYPE.STAT_MAR, true);
        map[43] = (STAT_TYPE.STAT_ADR, true);
        map[44] = (STAT_TYPE.STAT_PCP, true);
        map[45] = (STAT_TYPE.STAT_MCP, true);
        map[46] = (STAT_TYPE.STAT_PCRP, true);
        map[47] = (STAT_TYPE.STAT_MCRP, true);
        map[48] = (STAT_TYPE.STAT_PCR, true);
        map[49] = (STAT_TYPE.STAT_MCR, true);
        map[50] = (STAT_TYPE.STAT_PCA, true);
        map[51] = (STAT_TYPE.STAT_PCA, false);
        map[52] = (STAT_TYPE.STAT_MCA, true);
        map[53] = (STAT_TYPE.STAT_MCA, false);
        map[54] = (STAT_TYPE.STAT_DAR, true);
        map[55] = (STAT_TYPE.STAT_PDSR, true);
        map[56] = (STAT_TYPE.STAT_MDSR, true);
        map[57] = (STAT_TYPE.STAT_RES_BURN, true);
        map[58] = (STAT_TYPE.STAT_RES_POISON, true);
        map[59] = (STAT_TYPE.STAT_RES_SHOCK, true);
        map[60] = (STAT_TYPE.STAT_PARP, true);
        map[61] = (STAT_TYPE.STAT_MARP, true);
        map[62] = (STAT_TYPE.STAT_PASR, true);
        map[63] = (STAT_TYPE.STAT_MASR, true);

        // The reductions of damage between players are numbers, although their names say a share.
        map[64] = (STAT_TYPE.STAT_PDPR, true);
        map[65] = (STAT_TYPE.STAT_MDPR, true);

        // 66 is empty.
        map[67] = (STAT_TYPE.STAT_RES_CHARM, true);
        map[68] = (STAT_TYPE.STAT_RES_CONFUSION, true);
        map[69] = (STAT_TYPE.STAT_RES_BLEED, true);
        map[70] = (STAT_TYPE.STAT_RES_STUN, true);
        map[71] = (STAT_TYPE.STAT_RES_PARALYSIS, true);
        map[72] = (STAT_TYPE.STAT_RES_SLEEP, true);
        map[73] = (STAT_TYPE.STAT_RES_FREEZE, true);
        map[74] = (STAT_TYPE.STAT_RES_SILENCE, true);
        map[75] = (STAT_TYPE.STAT_RES_WEAKNESS, true);
        map[76] = (STAT_TYPE.STAT_ITEM_ATTACK, true);
        map[77] = (STAT_TYPE.STAT_ITEM_ATTACK, false);
        map[78] = (STAT_TYPE.STAT_ITEM_DEFENSE, true);
        map[79] = (STAT_TYPE.STAT_ITEM_DEFENSE, false);

        // The attack of the six attributes and the resistance to them.
        for (var i = 0; i < 12; i++)
        {
            map[80 + i] = ((STAT_TYPE)((int)STAT_TYPE.STAT_ATTRIBUTE_LIGHT + i), true);
        }

        map[92] = (STAT_TYPE.STAT_PVP_ATK, true);
        map[93] = (STAT_TYPE.STAT_PVP_DEF, true);

        return map;
    }

    // The effects come in pairs: one adds a number, the other a share in percent. Only what the stats hold is here.
    public const int StrengthNumber = 1;
    public const int StrengthShare = 2;
    public const int AgilityNumber = 3;
    public const int AgilityShare = 4;
    public const int IntelligenceNumber = 5;
    public const int IntelligenceShare = 6;
    public const int BalanceNumber = 7;
    public const int BalanceShare = 8;
    public const int VitalityNumber = 9;
    public const int VitalityShare = 10;
    public const int LuckNumber = 11;
    public const int LuckShare = 12;
    public const int HealthNumber = 15;
    public const int HealthShare = 16;
    public const int PhysicalAttackNumber = 33;
    public const int PhysicalAttackShare = 34;
    public const int MagicalAttackNumber = 35;
    public const int MagicalAttackShare = 36;
    public const int PhysicalDefenseNumber = 37;
    public const int PhysicalDefenseShare = 38;
    public const int MagicalDefenseNumber = 39;
    public const int MagicalDefenseShare = 40;
    public const int PhysicalAccuracyNumber = 41;
    public const int MagicalAccuracyNumber = 42;
    public const int GrazeDamageNumber = 43;
    public const int PhysicalCriticalChanceNumber = 44;
    public const int MagicalCriticalChanceNumber = 45;
    public const int PhysicalCriticalResistNumber = 46;
    public const int MagicalCriticalResistNumber = 47;
    public const int PhysicalCriticalDamageNumber = 50;
    public const int PhysicalCriticalDamageShare = 51;
    public const int MagicalCriticalDamageNumber = 52;
    public const int MagicalCriticalDamageShare = 53;
    public const int PhysicalDamageReductionNumber = 55;
    public const int MagicalDamageReductionNumber = 56;
    public const int PhysicalEvasionNumber = 60;
    public const int MagicalEvasionNumber = 61;
    public const int MoveSpeedShare = 31;
    public const int AttackSpeedShare = 32;

    // The resistance to a state, in the order of the Resistances: burn, poison, shock, bleed, stun, paralysis, sleep, freeze,
    // charm, confusion, silence, weakness.
    private static readonly int[] ResistanceNumbers = [57, 58, 59, 69, 70, 71, 72, 73, 67, 68, 74, 75];

    private const int AttributeAttackFirst = 80;
    private const int AttributeResistFirst = 86;

    /// <summary>
    /// The characteristics with the effects on them. A characteristic is first given the numbers and then the shares, and
    /// whatever the stats do not hold (speed, resistances, bonus damage) is left to the ones that use it.
    /// </summary>
    public static CombatStats Apply(in CombatStats stats, ReadOnlySpan<StatEffect> effects)
    {
        if (effects.IsEmpty)
        {
            return stats;
        }

        return stats with
        {
            MoveSpeed = Sum(stats.MoveSpeed, effects, MoveSpeedShare),
            AttackSpeed = Sum(stats.AttackSpeed, effects, AttackSpeedShare),
            Resist = new Resistances
            {
                Burn = Sum(stats.Resist.Burn, effects, ResistanceNumbers[0]),
                Poison = Sum(stats.Resist.Poison, effects, ResistanceNumbers[1]),
                Shock = Sum(stats.Resist.Shock, effects, ResistanceNumbers[2]),
                Bleed = Sum(stats.Resist.Bleed, effects, ResistanceNumbers[3]),
                Stun = Sum(stats.Resist.Stun, effects, ResistanceNumbers[4]),
                Paralysis = Sum(stats.Resist.Paralysis, effects, ResistanceNumbers[5]),
                Sleep = Sum(stats.Resist.Sleep, effects, ResistanceNumbers[6]),
                Freeze = Sum(stats.Resist.Freeze, effects, ResistanceNumbers[7]),
                Charm = Sum(stats.Resist.Charm, effects, ResistanceNumbers[8]),
                Confusion = Sum(stats.Resist.Confusion, effects, ResistanceNumbers[9]),
                Silence = Sum(stats.Resist.Silence, effects, ResistanceNumbers[10]),
                Weakness = Sum(stats.Resist.Weakness, effects, ResistanceNumbers[11])
            },
            AttributeAttack = Attributes(stats.AttributeAttack, effects, AttributeAttackFirst),
            AttributeResist = Attributes(stats.AttributeResist, effects, AttributeResistFirst),
            Strength = Change(stats.Strength, effects, StrengthNumber, StrengthShare),
            Agility = Change(stats.Agility, effects, AgilityNumber, AgilityShare),
            Intelligence = Change(stats.Intelligence, effects, IntelligenceNumber, IntelligenceShare),
            Balance = Change(stats.Balance, effects, BalanceNumber, BalanceShare),
            Vitality = Change(stats.Vitality, effects, VitalityNumber, VitalityShare),
            Luck = Change(stats.Luck, effects, LuckNumber, LuckShare),
            MaxHealth = Change(stats.MaxHealth, effects, HealthNumber, HealthShare),
            GrazeDamage = Change(stats.GrazeDamage, effects, GrazeDamageNumber),
            Physical = stats.Physical with
            {
                AttackMax = Change(stats.Physical.AttackMax, effects, PhysicalAttackNumber, PhysicalAttackShare),
                Defense = Change(stats.Physical.Defense, effects, PhysicalDefenseNumber, PhysicalDefenseShare),
                Accuracy = Change(stats.Physical.Accuracy, effects, PhysicalAccuracyNumber),
                Evasion = Change(stats.Physical.Evasion, effects, PhysicalEvasionNumber),
                CriticalChance = Change(stats.Physical.CriticalChance, effects, PhysicalCriticalChanceNumber),
                CriticalResist = Change(stats.Physical.CriticalResist, effects, PhysicalCriticalResistNumber),
                CriticalDamage = Change(stats.Physical.CriticalDamage, effects, PhysicalCriticalDamageNumber, PhysicalCriticalDamageShare),
                DamageReduction = Change(stats.Physical.DamageReduction, effects, PhysicalDamageReductionNumber)
            },
            Magical = stats.Magical with
            {
                AttackMax = Change(stats.Magical.AttackMax, effects, MagicalAttackNumber, MagicalAttackShare),
                Defense = Change(stats.Magical.Defense, effects, MagicalDefenseNumber, MagicalDefenseShare),
                Accuracy = Change(stats.Magical.Accuracy, effects, MagicalAccuracyNumber),
                Evasion = Change(stats.Magical.Evasion, effects, MagicalEvasionNumber),
                CriticalChance = Change(stats.Magical.CriticalChance, effects, MagicalCriticalChanceNumber),
                CriticalResist = Change(stats.Magical.CriticalResist, effects, MagicalCriticalResistNumber),
                CriticalDamage = Change(stats.Magical.CriticalDamage, effects, MagicalCriticalDamageNumber, MagicalCriticalDamageShare),
                DamageReduction = Change(stats.Magical.DamageReduction, effects, MagicalDamageReductionNumber)
            }
        };
    }

    private static AttributeStats Attributes(in AttributeStats value, ReadOnlySpan<StatEffect> effects, int first)
    {
        return new AttributeStats
        {
            Light = Sum(value.Light, effects, first),
            Darkness = Sum(value.Darkness, effects, first + 1),
            Cool = Sum(value.Cool, effects, first + 2),
            Abhor = Sum(value.Abhor, effects, first + 3),
            Heal = Sum(value.Heal, effects, first + 4),
            Pain = Sum(value.Pain, effects, first + 5)
        };
    }

    /// <summary>
    /// A stat that only takes numbers added to it (a speed, a resistance, an attack of an attribute).
    /// </summary>
    private static float Sum(float value, ReadOnlySpan<StatEffect> effects, int effect)
    {
        foreach (var candidate in effects)
        {
            if (candidate.Type == effect)
            {
                value += candidate.Value;
            }
        }

        return value;
    }

    private static float Change(float value, ReadOnlySpan<StatEffect> effects, int number, int share = 0)
    {
        var added = 0f;
        var percent = 0f;
        var touched = false;

        foreach (var effect in effects)
        {
            if (effect.Type == number)
            {
                added += effect.Value;
                touched = true;
            }

            else if (share != 0 && effect.Type == share)
            {
                percent += effect.Value;
                touched = true;
            }
        }

        return touched ? (value + added) * (1f + percent / 100f) : value;
    }
}
