using OpenWorker.Gameplay.Combat;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class DamageCalculatorTest
{
    private sealed class FixedRandom(int roll) : Random
    {
        public override int Next(int maxValue) => Math.Min(roll, maxValue - 1);

        public override int Next(int minValue, int maxValue) => minValue;
    }

    // Never rolls a critical, and hits whenever the chance is above zero.
    private static readonly Random Plain = new FixedRandom(9000);
    private static readonly Random Lucky = new FixedRandom(0);

    private static readonly DamageStats Weapon = new()
    {
        AttackMin = 100,
        AttackMax = 100,
        Accuracy = 1000,
        CriticalDamage = 50
    };

    private static readonly CombatStats Attacker = new() { Level = 1, GrazeDamage = 50, Physical = Weapon, Magical = Weapon with { AttackMin = 200, AttackMax = 200 } };
    private static readonly CombatStats Target = new() { Level = 1 };
    private static readonly SkillDamage Skill = new(1, 0, 0, 100);

    [TestMethod]
    public void DefenseScalesWithTheLevelOfTheTarget()
    {
        // Without defense a single point of armor stands against 50 per level.
        Assert.AreEqual(new DamageResult(98, DamageFlags.None), DamageCalculator.Calculate(Attacker, Target, Skill, DefenseType.Normal, 1f, Plain));

        var armored = Target with { Physical = new DamageStats { Defense = 50 } };

        Assert.AreEqual(50, DamageCalculator.Calculate(Attacker, armored, Skill, DefenseType.Normal, 1f, Plain).Damage);

        var higher = armored with { Level = 3 };

        Assert.AreEqual(75, DamageCalculator.Calculate(Attacker, higher, Skill, DefenseType.Normal, 1f, Plain).Damage);
    }

    [TestMethod]
    public void SkillAddsToTheHitAndThenScalesIt()
    {
        var skill = new SkillDamage(1, 2, 1, 50);

        Assert.AreEqual(50, DamageCalculator.Calculate(Attacker, Target, skill, DefenseType.Normal, 1f, Plain).Damage);
    }

    [TestMethod]
    public void MagicalSkillsUseTheMagicalSide()
    {
        var skill = Skill with { Attribute = SkillDamage.MagicalAttribute };

        Assert.AreEqual(196, DamageCalculator.Calculate(Attacker, Target, skill, DefenseType.Normal, 1f, Plain).Damage);
    }

    [TestMethod]
    public void AttackBelowEvasionMissesAndGrazes()
    {
        var blind = Attacker with { Physical = Weapon with { Accuracy = 0 } };
        var result = DamageCalculator.Calculate(blind, Target, Skill, DefenseType.Normal, 1f, Lucky);

        Assert.AreEqual(DamageFlags.Miss, result.Flags);
        Assert.AreEqual(49, result.Damage);
    }

    [TestMethod]
    public void EvasionLowersTheChanceToHit()
    {
        // The chance is a tenth of the difference, in hundredths of a percent.
        var dodging = Target with { Physical = new DamageStats { Evasion = 900 } };

        Assert.AreEqual(DamageFlags.Miss, DamageCalculator.Calculate(Attacker, dodging, Skill, DefenseType.Normal, 1f, new FixedRandom(1000)).Flags & DamageFlags.Miss);
        Assert.AreEqual(DamageFlags.None, DamageCalculator.Calculate(Attacker, dodging, Skill, DefenseType.Normal, 1f, new FixedRandom(999)).Flags & DamageFlags.Miss);
    }

    [TestMethod]
    public void CriticalAddsItsDamage()
    {
        var result = DamageCalculator.Calculate(Attacker, Target, Skill, DefenseType.Normal, 1f, Lucky);

        Assert.AreEqual(DamageFlags.Critical, result.Flags);
        Assert.AreEqual(147, result.Damage);
    }

    [TestMethod]
    public void ReductionTakesAShareBeforeDefense()
    {
        var protectedTarget = Target with { Physical = new DamageStats { DamageReduction = 50 } };

        Assert.AreEqual(49, DamageCalculator.Calculate(Attacker, protectedTarget, Skill, DefenseType.Normal, 1f, Plain).Damage);
    }

    [TestMethod]
    public void InvincibleTakesNothingAndGuardAFifth()
    {
        Assert.AreEqual(new DamageResult(0, DamageFlags.None), DamageCalculator.Calculate(Attacker, Target, Skill, DefenseType.Invincible, 1f, Plain));
        Assert.AreEqual(19, DamageCalculator.Calculate(Attacker, Target, Skill, DefenseType.Guard, 1f, Plain).Damage);
    }

    [TestMethod]
    public void AttackRateScalesTheHitBeforeTheSkill()
    {
        var skill = Skill with { BasicDamage = 10 };

        Assert.AreEqual(59, DamageCalculator.Calculate(Attacker, Target, skill, DefenseType.Normal, 0.5f, Plain).Damage);
        Assert.AreEqual(108, DamageCalculator.Calculate(Attacker, Target, skill, DefenseType.Normal, 0f, Plain).Damage);
    }

    [TestMethod]
    public void AHitAlwaysDoesAtLeastOne()
    {
        var weak = Attacker with { Physical = Weapon with { AttackMin = 0, AttackMax = 0 } };

        Assert.AreEqual(1, DamageCalculator.Calculate(weak, Target, Skill with { DamageRate = 1 }, DefenseType.Normal, 1f, Plain).Damage);
    }
}
