using OpenWorker.Batch.Actions;
using OpenWorker.Gameplay.Combat;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// How a character takes hits at a moment of the animation it plays.
/// </summary>
[TestClass]
public sealed class DefenseFramesTest
{
    private static DefenseType In(byte animation, DefenseType own = DefenseType.Normal, DefenseEvent[]? stretches = null, float elapsed = 0.5f, bool broken = false)
    {
        return DefenseFrames.Of(own, animation, stretches, elapsed, broken);
    }

    /// <summary>
    /// An animation that says nothing leaves a character as its table has it; one that says plain, armored or invincible
    /// makes it so, whatever the table says. This is how a hero is armored through a skill and out of reach through a roll.
    /// </summary>
    [TestMethod]
    public void AnAnimationSetsTheDefenseAndOneThatSaysNothingLeavesIt()
    {
        Assert.AreEqual(DefenseType.SuperArmor, In(ActionDefense.Unchanged, DefenseType.SuperArmor));
        Assert.AreEqual(DefenseType.Normal, In(ActionDefense.Unchanged));
        Assert.AreEqual(DefenseType.SuperArmor, In(ActionDefense.SuperArmor));
        Assert.AreEqual(DefenseType.Invincible, In(ActionDefense.Invincible, DefenseType.SuperArmor));
        Assert.AreEqual(DefenseType.Normal, In(ActionDefense.Normal, DefenseType.SuperArmor), "a plain animation takes the armor off");
        Assert.AreEqual(DefenseType.NoReaction, In((byte)DefenseType.NoReaction));
    }

    /// <summary>
    /// A stretch of the animation says its own for the time it lasts, from where it begins to where it ends.
    /// </summary>
    [TestMethod]
    public void AStretchOfTheAnimationSaysItsOwnForTheTimeItLasts()
    {
        DefenseEvent[] stretch = [new DefenseEvent(0.2f, 0.6f, ActionDefense.Invincible, false)];

        Assert.AreEqual(DefenseType.Normal, In(ActionDefense.Unchanged, stretches: stretch, elapsed: 0.1f));
        Assert.AreEqual(DefenseType.Invincible, In(ActionDefense.Unchanged, stretches: stretch, elapsed: 0.2f));
        Assert.AreEqual(DefenseType.Normal, In(ActionDefense.Unchanged, stretches: stretch, elapsed: 0.6f));

        DefenseEvent[] open = [new DefenseEvent(0.2f, -1f, ActionDefense.SuperArmor, false)];

        Assert.AreEqual(DefenseType.SuperArmor, In(ActionDefense.Unchanged, stretches: open, elapsed: 3f), "a stretch with no end lasts the rest of the animation");
        Assert.AreEqual(DefenseType.Normal, In(ActionDefense.Unchanged, DefenseType.SuperArmor, [new DefenseEvent(0f, 1f, ActionDefense.Normal, false)]), "a stretch can take the armor off");
    }

    /// <summary>
    /// Once the armor is broken the stretches say nothing: only the animation does.
    /// </summary>
    [TestMethod]
    public void ABrokenArmorIsNotRestoredByAStretch()
    {
        DefenseEvent[] stretch = [new DefenseEvent(0f, 1f, ActionDefense.SuperArmor, false)];

        Assert.AreEqual(DefenseType.Normal, In(ActionDefense.Unchanged, stretches: stretch, broken: true));
        Assert.AreEqual(DefenseType.SuperArmor, In(ActionDefense.SuperArmor, stretches: stretch, broken: true), "the animation still counts");
    }
}
