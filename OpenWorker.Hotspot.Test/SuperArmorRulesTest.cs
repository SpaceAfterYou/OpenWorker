using OpenWorker.Gameplay.Combat;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The gauge of the super armor of a monster: what a hit takes from it, when it breaks, and how long it stays broken.
/// </summary>
[TestClass]
public sealed class SuperArmorRulesTest
{
    /// <summary>
    /// A hit takes the share of its damage the skill gives to the armor: all of it for 100, none for a skill with no share.
    /// </summary>
    [TestMethod]
    public void AHitTakesTheShareOfItsDamageTheSkillGivesToTheArmor()
    {
        Assert.AreEqual(1000f, SuperArmorRules.Damage(1000, 100f));
        Assert.AreEqual(500f, SuperArmorRules.Damage(1000, 50f));
        Assert.AreEqual(0f, SuperArmorRules.Damage(1000, 0f));
        Assert.AreEqual(0f, SuperArmorRules.Damage(0, 100f));
    }

    /// <summary>
    /// The armor holds while the gauge lasts and breaks when it is empty; a hard blow breaks it with the gauge full.
    /// </summary>
    [TestMethod]
    public void TheArmorBreaksWhenTheGaugeIsEmptyOrAtOnceForAHardBlow()
    {
        Assert.AreEqual(new ArmorHit(700f, false), SuperArmorRules.Take(1000f, 300f, 1));
        Assert.AreEqual(new ArmorHit(0f, true), SuperArmorRules.Take(300f, 300f, 1));
        Assert.AreEqual(new ArmorHit(0f, true), SuperArmorRules.Take(1000f, 0f, 3));
        Assert.AreEqual(new ArmorHit(0f, true), SuperArmorRules.Take(1000f, 0f, 4));
    }

    /// <summary>
    /// A broken armor stays broken for the animations of the break and the time of the group, three seconds when it has none.
    /// </summary>
    [TestMethod]
    public void ABrokenArmorStaysBrokenForItsAnimationsAndTheRecoveryTime()
    {
        Assert.AreEqual(0.6f + 0.9f + 4f, SuperArmorRules.BreakSeconds(0.6f, 0.9f, 4000), 0.001f);
        Assert.AreEqual(3f, SuperArmorRules.BreakSeconds(0f, 0f, 0), 0.001f);
    }
}
