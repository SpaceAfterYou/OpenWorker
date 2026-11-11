using OpenWorker.Gameplay.Ai;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// How a monster that stands begins to walk.
/// </summary>
[TestClass]
public sealed class WalkStartTest
{
    /// <summary>
    /// Within a quarter of a turn from the way it goes a monster sets off at once, whatever animations it has.
    /// </summary>
    [TestMethod]
    public void AMonsterNearlyFacingItsWaySetsOffAtOnce()
    {
        Assert.AreEqual(WalkStarts.Walk, WalkStart.Of(10f, 55f, true, true, out _));
        Assert.AreEqual(WalkStarts.Walk, WalkStart.Of(170f, -160f, false, false, out _), "the angle goes round");
    }

    /// <summary>
    /// A monster with the animations of turning turns where it stands, to the side the way lies; beyond a quarter of a turn
    /// again it uses the big turn if it has one. One without them turns at once and the clients wait a moment.
    /// </summary>
    [TestMethod]
    public void AMonsterTurnsWithItsAnimationOrWaitsAMomentWithout()
    {
        Assert.AreEqual(WalkStarts.Turn, WalkStart.Of(0f, 60f, true, true, out var left));
        Assert.IsTrue(left);
        Assert.AreEqual(WalkStarts.Turn, WalkStart.Of(0f, -60f, true, true, out left));
        Assert.IsFalse(left);
        Assert.AreEqual(WalkStarts.BigTurn, WalkStart.Of(0f, 120f, true, true, out _));
        Assert.AreEqual(WalkStarts.Turn, WalkStart.Of(0f, 120f, true, false, out _), "no animation for a big turn");
        Assert.AreEqual(WalkStarts.Pause, WalkStart.Of(0f, 120f, false, false, out _));
        Assert.AreEqual(0.3f, WalkStart.PauseSeconds);
    }
}
