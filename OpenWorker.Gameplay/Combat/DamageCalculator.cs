using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Combat;

[Flags]
public enum HitResults : byte
{
    None = 0,
    Miss = 1,
    Critical = 4
}

/// <summary>
/// How a character takes hits.
/// </summary>
public enum DefenseType : byte
{
    Normal = 0,
    Guard = 1,
    SuperArmor = 2,
    Invincible = 3,

    /// <summary>
    /// Hurt as a plain target but shows no reaction to any blow: an animation of a monster can give it.
    /// </summary>
    NoReaction = 5
}

/// <param name="SoulGaugeAbsorb">What a hit of the skill gives to the soul gauge of the attacker, before the share of the target.</param>
public readonly record struct SkillDamage(byte Attribute, float BasicDamage, float AddDamage, float DamageRate, float SuperArmorAttack = 0f, float SoulGaugeAbsorb = 0f)
{
    public const byte MagicalAttribute = 2;

    public static SkillDamage From(in SkillRow row)
    {
        return new SkillDamage(row.Attribute, row.BasicDamage, row.AddDamage, row.DamageRate, row.SuperArmorAttack, row.SoulGaugeAbsorb);
    }
}

public readonly record struct DamageResult(int Damage, HitResults Flags);

public static class DamageCalculator
{
    private const int Certain = 10000;
    private const float DefenseBaseByLevel = 50f;
    private const float GuardedDamage = 0.2f;
    private const float Percent = 0.01f;
    private const float BrokenSuperArmorDamage = 1.5f;

    public static DamageResult Calculate(
        in CombatStats attacker,
        in CombatStats target,
        in SkillDamage skill,
        DefenseType defense,
        float attackRate,
        Random random,
        bool superArmorBroken = false,
        in DamageContext context = default)
    {
        if (defense == DefenseType.Invincible)
        {
            return new DamageResult(0, HitResults.None);
        }

        var magical = skill.Attribute == SkillDamage.MagicalAttribute;
        var own = magical ? attacker.Magical : attacker.Physical;
        var other = magical ? target.Magical : target.Physical;
        var flags = HitResults.None;
        var accuracyEdge = 0f;

        var hitChance = (int)((own.Accuracy - other.Evasion) * 100f);

        if (hitChance > 0)
        {
            hitChance /= 10;
        }

        if (Math.Clamp(hitChance, 0, Certain) > random.Next(Certain))
        {
            var accuracy = own.Accuracy > 0 ? own.Accuracy / 10f : own.Accuracy;
            var evasion = other.Evasion > 0 ? other.Evasion / 10f : other.Evasion;
            var difference = accuracy - evasion;

            accuracyEdge = difference <= 0 ? 1f : difference / 5f;
        }
        else
        {
            flags |= HitResults.Miss;
        }

        if ((flags & HitResults.Miss) == 0)
        {
            var criticalChance = 100 * (int)(own.CriticalChance + accuracyEdge - other.CriticalResist);

            if (Math.Clamp(criticalChance, 0, Certain) > random.Next(Certain))
            {
                flags |= HitResults.Critical;
            }
        }

        float result = RollAttack(own, random);

        if ((flags & HitResults.Miss) != 0)
        {
            result *= attacker.GrazeDamage * Percent;
        }

        if ((flags & HitResults.Critical) != 0)
        {
            result += own.CriticalDamage;
        }

        result -= result * ((own.ClassDamageReduction + other.DamageReduction) * Percent);

        var afterDefense = ApplyDefense((int)result, other.Defense, target.Level, defense);

        // A monster whose super armor is broken takes half as much again.
        if (superArmorBroken)
        {
            afterDefense = (int)(afterDefense * BrokenSuperArmorDamage);
        }

        // What gear adds to the damage to a kind of monster, and takes off the damage of one.
        afterDefense = SpecialDamage.Apply(afterDefense, attacker, target, context, (flags & HitResults.Miss) != 0);

        if (attackRate > 0)
        {
            afterDefense = (int)(afterDefense * attackRate);
        }

        var withSkill = (afterDefense + skill.BasicDamage + skill.AddDamage) * (skill.DamageRate * Percent);
        var damage = (int)withSkill;

        return new DamageResult(damage <= 0 ? 1 : damage, flags);
    }

    private static int RollAttack(in DamageStats own, Random random)
    {
        var min = (int)own.AttackMin;
        var max = (int)own.AttackMax;

        return max > min ? random.Next(min, max + 1) : min;
    }

    private static int ApplyDefense(int attack, float defense, byte level, DefenseType type)
    {
        var armor = Math.Max(0f, defense);

        if (armor <= 0)
        {
            armor = 1f;
        }

        var damage = (int)(attack * (1f - armor / (armor + DefenseBaseByLevel * level)));

        if (type == DefenseType.Guard)
        {
            damage = (int)(damage * GuardedDamage);
        }

        return damage <= 0 ? 1 : damage;
    }
}
