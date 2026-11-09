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

public static class CombatStatCalculator
{
    private const byte RangedStatusType = 2;
    private const float AttackPerStat = 10f;
    private const float AttackMinShare = 0.8f;

    public static CombatStats Calculate(in StatusRow status, byte level, byte statusType, in StatBonuses bonuses = default)
    {
        var strength = Base(status.StrengthFirst, status.StrengthGrowth, level);
        var agility = Base(status.AgilityFirst, status.AgilityGrowth, level);
        var intelligence = Base(status.IntelligenceFirst, status.IntelligenceGrowth, level);
        var balance = Base(status.BalanceFirst, status.BalanceGrowth, level);
        var vitality = Base(status.VitalityFirst, status.VitalityGrowth, level);
        var luck = Base(status.LuckFirst, status.LuckGrowth, level);

        var physicalStat = statusType == RangedStatusType ? agility : strength;
        var physicalAttack = physicalStat * AttackPerStat * status.PhysicalAttackRate + bonuses.PhysicalAttack;
        var magicalAttack = intelligence * AttackPerStat * status.MagicalAttackRate + bonuses.MagicalAttack;

        return new CombatStats
        {
            Level = level,
            Strength = strength,
            Agility = agility,
            Intelligence = intelligence,
            Balance = balance,
            Vitality = vitality,
            Luck = luck,
            MaxHealth = status.HealthFirst + vitality * status.HealthPerVitality + bonuses.Health,
            GrazeDamage = status.GrazeDamage + bonuses.GrazeDamage,
            SoulGaugeConstant = status.SoulGaugeRegen,
            SoulGaugeAbsorbRate = status.SoulGaugeAbsorbRate,
            StaminaMax = status.StaminaFirst,
            StaminaRegen = status.StaminaRegen,
            Physical = new DamageStats
            {
                AttackMax = physicalAttack,
                AttackMin = physicalAttack * AttackMinShare,
                Defense = status.PhysicalDefenseRate * agility + bonuses.PhysicalDefense,
                Accuracy = (status.PhysicalAccuracyFirst + intelligence * 0.125f * status.PhysicalAccuracyRate) * 10f + bonuses.PhysicalAccuracy,
                Evasion = balance * 0.025f * status.PhysicalEvasionRate * 10f + bonuses.PhysicalEvasion,
                CriticalChance = status.PhysicalCriticalChance + bonuses.PhysicalCriticalChance,
                CriticalResist = status.PhysicalCriticalResist + bonuses.PhysicalCriticalResist,
                CriticalDamage = physicalAttack * status.PhysicalCriticalDamageRate + bonuses.PhysicalCriticalDamage,
                DamageReduction = status.PhysicalDamageReduction + bonuses.PhysicalDamageReduction,
                ClassDamageReduction = status.PhysicalDamageReduction
            },
            Magical = new DamageStats
            {
                AttackMax = magicalAttack,
                AttackMin = magicalAttack * AttackMinShare,
                Defense = (balance * 0.5f + intelligence * 2f) * status.MagicalDefenseRate + bonuses.MagicalDefense,
                Accuracy = status.MagicalAccuracyFirst + intelligence * 0.125f * status.MagicalAccuracyRate + bonuses.MagicalAccuracy,
                Evasion = (balance * 0.025f + intelligence * 0.025f) * status.MagicalAccuracyRate + bonuses.MagicalEvasion,
                CriticalChance = (balance * 0.05f + intelligence * 0.075f) * status.MagicalCriticalChanceRate + bonuses.MagicalCriticalChance,
                CriticalResist = intelligence * 0.1f * status.MagicalCriticalResistRate + bonuses.MagicalCriticalResist,
                CriticalDamage = magicalAttack * status.MagicalCriticalDamageRate * 0.01f,
                DamageReduction = status.MagicalDamageReduction + bonuses.MagicalDamageReduction,
                ClassDamageReduction = status.MagicalDamageReduction
            }
        };
    }

    /// <summary>
    /// A stat at a level, rounded the way the tables are meant to be read: the first value is for level one.
    /// </summary>
    private static float Base(float first, float growth, byte level)
    {
        return MathF.Floor(first + (level - 1) * growth + 0.5f);
    }
}
