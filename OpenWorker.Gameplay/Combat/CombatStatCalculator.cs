using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// Flat bonuses on top of what a class gives. Monsters have them in their table, players get them from gear.
/// </summary>
public readonly record struct StatBonuses
{
    public float Health { get; init; }
    public float PhysicalAttack { get; init; }
    public float MagicalAttack { get; init; }
    public float PhysicalDefense { get; init; }
    public float MagicalDefense { get; init; }
    public float PhysicalAccuracy { get; init; }
    public float MagicalAccuracy { get; init; }
    public float PhysicalEvasion { get; init; }
    public float MagicalEvasion { get; init; }
    public float GrazeDamage { get; init; }
    public float PhysicalCriticalChance { get; init; }
    public float MagicalCriticalChance { get; init; }
    public float PhysicalCriticalResist { get; init; }
    public float MagicalCriticalResist { get; init; }
    public float PhysicalCriticalDamage { get; init; }
    public float PhysicalDamageReduction { get; init; }
    public float MagicalDamageReduction { get; init; }

    public static StatBonuses From(in MonsterRow row)
    {
        return new StatBonuses
        {
            Health = row.AddHealth,
            PhysicalAttack = row.AddPhysicalAttack,
            MagicalAttack = row.AddMagicalAttack,
            PhysicalDefense = row.AddPhysicalDefense,
            MagicalDefense = row.AddMagicalDefense,
            PhysicalAccuracy = row.AddPhysicalAccuracy,
            MagicalAccuracy = row.AddMagicalAccuracy,
            PhysicalEvasion = row.AddPhysicalEvasion,
            MagicalEvasion = row.AddMagicalEvasion,
            GrazeDamage = row.AddGrazeDamage,
            // The original leaves the physical critical chance of the row out, and takes the magical one.
            MagicalCriticalChance = row.AddMagicalCriticalChance,
            PhysicalCriticalResist = row.AddPhysicalCriticalResist,
            MagicalCriticalResist = row.AddMagicalCriticalResist,
            PhysicalCriticalDamage = row.AddPhysicalCriticalDamage,
            PhysicalDamageReduction = row.AddPhysicalDamageReduction,
            MagicalDamageReduction = row.AddMagicalDamageReduction
        };
    }
}

/// <summary>
/// The stats of a character from its status at its level and from what is added to them (<see cref="StatSheet"/>), by the
/// formulas of the game: the main stats take the numbers added to them, and every other stat is worked out from the main
/// stats with them, then grows by its share and takes its number.
/// </summary>
public static class CombatStatCalculator
{
    private const byte RangedStatusType = 2;
    private const float AttackPerStat = 10f;
    private const float AttackMinShare = 0.8f;
    private const float Percent = 0.01f;
    private const float SpeedBase = 100f;

    private static readonly StatSheet Nothing = new();

    private static Resistances ResistancesOf(StatSheet sheet)
    {
        return new Resistances
        {
            Burn = sheet.Added(STAT_TYPE.STAT_RES_BURN),
            Poison = sheet.Added(STAT_TYPE.STAT_RES_POISON),
            Shock = sheet.Added(STAT_TYPE.STAT_RES_SHOCK),
            Bleed = sheet.Added(STAT_TYPE.STAT_RES_BLEED),
            Stun = sheet.Added(STAT_TYPE.STAT_RES_STUN),
            Paralysis = sheet.Added(STAT_TYPE.STAT_RES_PARALYSIS),
            Sleep = sheet.Added(STAT_TYPE.STAT_RES_SLEEP),
            Freeze = sheet.Added(STAT_TYPE.STAT_RES_FREEZE),
            Charm = sheet.Added(STAT_TYPE.STAT_RES_CHARM),
            Confusion = sheet.Added(STAT_TYPE.STAT_RES_CONFUSION),
            Silence = sheet.Added(STAT_TYPE.STAT_RES_SILENCE),
            Weakness = sheet.Added(STAT_TYPE.STAT_RES_WEAKNESS)
        };
    }

    public static CombatStats Calculate(in StatusRow status, byte level, byte statusType, in StatBonuses bonuses = default)
    {
        return Calculate(status, level, statusType, bonuses == default ? Nothing : StatSheet.From(bonuses));
    }

    public static CombatStats Calculate(in StatusRow status, byte level, byte statusType, StatSheet sheet)
    {
        var strength = Base(status.StrengthFirst, status.StrengthGrowth, level) + sheet.Added(STAT_TYPE.STAT_STR);
        var agility = Base(status.AgilityFirst, status.AgilityGrowth, level) + sheet.Added(STAT_TYPE.STAT_AGI);
        var intelligence = Base(status.IntelligenceFirst, status.IntelligenceGrowth, level) + sheet.Added(STAT_TYPE.STAT_INT);
        var balance = Base(status.BalanceFirst, status.BalanceGrowth, level) + sheet.Added(STAT_TYPE.STAT_BAL);
        var vitality = Base(status.VitalityFirst, status.VitalityGrowth, level) + sheet.Added(STAT_TYPE.STAT_VIT);
        var luck = Base(status.LuckFirst, status.LuckGrowth, level) + sheet.Added(STAT_TYPE.STAT_LUC);

        var physicalStat = statusType == RangedStatusType ? agility : strength;
        var physicalAttack = Final(physicalStat * AttackPerStat * status.PhysicalAttackRate, sheet, STAT_TYPE.STAT_PATK_MAX);

        // The magical attack, the magical defence and the magical evasion take their number before their share.
        var magicalAttack = Grown(intelligence * AttackPerStat * status.MagicalAttackRate, sheet, STAT_TYPE.STAT_MATK_MAX);

        // The physical critical damage grows by its share of the attack the strength gives, whatever the hero.
        var criticalBase = strength * AttackPerStat * status.PhysicalAttackRate * status.PhysicalCriticalDamageRate;

        return new CombatStats
        {
            Level = level,
            Strength = strength,
            Agility = agility,
            Intelligence = intelligence,
            Balance = balance,
            Vitality = vitality,
            Luck = luck,
            MaxHealth = Final(status.HealthFirst + vitality * status.HealthPerVitality, sheet, STAT_TYPE.STAT_HP_MAX),
            GrazeDamage = status.GrazeDamage + sheet.Added(STAT_TYPE.STAT_ADR),
            SoulGaugeConstant = status.SoulGaugeRegen,
            SoulGaugeAbsorbRate = status.SoulGaugeAbsorbRate,
            SoulGaugeRegenAdded = sheet.Added(STAT_TYPE.STAT_SG_REG),
            SoulGaugeRegenShare = sheet.Share(STAT_TYPE.STAT_SG_REG),
            StaminaMax = status.StaminaFirst + sheet.Added(STAT_TYPE.STAT_ST_MAX),
            StaminaRegen = Final(status.StaminaRegen, sheet, STAT_TYPE.STAT_ST_REG),

            // The speeds are 100 times the constant of the class, and what is added to them counts in percent.
            MoveSpeed = SpeedBase * status.MoveSpeedRate + sheet.Share(STAT_TYPE.STAT_MSR),
            AttackSpeed = SpeedBase * status.AttackSpeedRate + sheet.Share(STAT_TYPE.STAT_ASR),
            Resist = ResistancesOf(sheet),
            AttributeAttack = new AttributeStats
            {
                Light = sheet.Added(STAT_TYPE.STAT_ATTRIBUTE_LIGHT),
                Darkness = sheet.Added(STAT_TYPE.STAT_ATTRIBUTE_DARKNESS),
                Cool = sheet.Added(STAT_TYPE.STAT_ATTRIBUTE_COOL),
                Abhor = sheet.Added(STAT_TYPE.STAT_ATTRIBUTE_ABHOR),
                Heal = sheet.Added(STAT_TYPE.STAT_ATTRIBUTE_HEAL),
                Pain = sheet.Added(STAT_TYPE.STAT_ATTRIBUTE_PAIN)
            },
            AttributeResist = new AttributeStats
            {
                Light = sheet.Added(STAT_TYPE.STAT_ATTRIBUTE_RES_LIGHT),
                Darkness = sheet.Added(STAT_TYPE.STAT_ATTRIBUTE_RES_DARKNESS),
                Cool = sheet.Added(STAT_TYPE.STAT_ATTRIBUTE_RES_COOL),
                Abhor = sheet.Added(STAT_TYPE.STAT_ATTRIBUTE_RES_ABHOR),
                Heal = sheet.Added(STAT_TYPE.STAT_ATTRIBUTE_RES_HEAL),
                Pain = sheet.Added(STAT_TYPE.STAT_ATTRIBUTE_RES_PAIN)
            },
            Physical = new DamageStats
            {
                AttackMax = physicalAttack,
                AttackMin = physicalAttack * AttackMinShare,
                Defense = Final(status.PhysicalDefenseRate * agility, sheet, STAT_TYPE.STAT_PDEF),
                Accuracy = (status.PhysicalAccuracyFirst + intelligence * 0.125f * status.PhysicalAccuracyRate) * 10f + sheet.Added(STAT_TYPE.STAT_PAR),
                Evasion = balance * 0.025f * status.PhysicalEvasionRate * 10f + sheet.Added(STAT_TYPE.STAT_PARP),
                CriticalChance = status.PhysicalCriticalChance + sheet.Added(STAT_TYPE.STAT_PCP),
                CriticalResist = status.PhysicalCriticalResist + sheet.Added(STAT_TYPE.STAT_PCRP),
                CriticalDamage = physicalAttack * status.PhysicalCriticalDamageRate
                    + criticalBase * Percent * sheet.Share(STAT_TYPE.STAT_PCA)
                    + sheet.Added(STAT_TYPE.STAT_PCA),
                DamageReduction = status.PhysicalDamageReduction + sheet.Added(STAT_TYPE.STAT_PDSR),
                ClassDamageReduction = status.PhysicalDamageReduction
            },
            Magical = new DamageStats
            {
                AttackMax = magicalAttack,
                AttackMin = magicalAttack * AttackMinShare,
                Defense = Grown((balance * 0.5f + intelligence * 2f) * status.MagicalDefenseRate, sheet, STAT_TYPE.STAT_MDEF),
                Accuracy = status.MagicalAccuracyFirst + intelligence * 0.125f * status.MagicalAccuracyRate + sheet.Added(STAT_TYPE.STAT_MAR),
                Evasion = Grown((balance * 0.025f + intelligence * 0.025f) * status.MagicalAccuracyRate, sheet, STAT_TYPE.STAT_MARP),
                CriticalChance = (balance * 0.05f + intelligence * 0.075f) * status.MagicalCriticalChanceRate + sheet.Added(STAT_TYPE.STAT_MCP),
                CriticalResist = intelligence * 0.1f * status.MagicalCriticalResistRate + sheet.Added(STAT_TYPE.STAT_MCRP),

                // As the game has it: the share is added to a hundredth, not divided by a hundred.
                CriticalDamage = magicalAttack * status.MagicalCriticalDamageRate * (sheet.Share(STAT_TYPE.STAT_MCA) + Percent),
                DamageReduction = status.MagicalDamageReduction + sheet.Added(STAT_TYPE.STAT_MDSR),
                ClassDamageReduction = status.MagicalDamageReduction
            }
        };
    }

    /// <summary>
    /// The soul gauge of a character whose size the server's own status table gives: the size grows by its share and takes its
    /// number, and what a second adds when the gauge is full follows the rule of the weapon of the hero.
    /// </summary>
    public static CombatStats WithSoulGauge(in CombatStats stats, Hero hero, float first, StatSheet sheet)
    {
        var maximum = Final(first, sheet, STAT_TYPE.STAT_SG_MAX);

        return stats with
        {
            SoulGaugeMax = maximum,
            SoulGaugeRegen = stats.SoulGaugeRegenOf(hero, maximum)
        };
    }

    /// <summary>
    /// A stat that grows by its share and then takes its number.
    /// </summary>
    private static float Final(float value, StatSheet sheet, STAT_TYPE stat)
    {
        return value * (1f + sheet.Share(stat) * Percent) + sheet.Added(stat);
    }

    /// <summary>
    /// A stat that takes its number and then grows by its share.
    /// </summary>
    private static float Grown(float value, StatSheet sheet, STAT_TYPE stat)
    {
        return (value + sheet.Added(stat)) * (1f + sheet.Share(stat) * Percent);
    }

    /// <summary>
    /// A stat at a level, rounded the way the tables are meant to be read: the first value is for level one.
    /// </summary>
    private static float Base(float first, float growth, byte level)
    {
        return MathF.Floor(first + (level - 1) * growth + 0.5f);
    }
}
