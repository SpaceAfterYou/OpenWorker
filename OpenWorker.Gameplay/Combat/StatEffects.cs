using OpenWorker.Gameplay.Modules.Persons;

namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// What an effect that comes from a title (or from gear) does to the characteristics of a character.
/// </summary>
public static class StatEffects
{
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
