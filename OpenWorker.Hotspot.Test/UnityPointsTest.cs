using OpenWorker.Gameplay.Modules.UnityPoints;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class UnityPointsTest
{
    [TestMethod]
    public void AComboGivesItsPointsOnceWhenItIsOverInTheMode()
    {
        var tally = new UnityTally();

        Assert.IsFalse(tally.CountCombo(50, 500, 3000, 0), "a combo still going counts nothing");
        Assert.IsTrue(tally.CountCombo(50, 500, 3500, 0));
        Assert.AreEqual(250, tally.BattlePoints, "a tenth of the square of the combo");
        Assert.IsFalse(tally.CountCombo(50, 500, 9000, 0), "a combo counts once");
        Assert.IsFalse(tally.CountCombo(20, 10_000, 20_000, 14_000), "a combo over before the mode counts nothing");
        Assert.AreEqual(250, tally.TakeBattlePoints());
        Assert.AreEqual(0, tally.BattlePoints, "paid points are gone");
    }
}
