using OpenWorker.Gameplay.Combat;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The combo of a player: it goes on while each attack hits within three seconds of the last, counts at most five targets
/// an attack, and starts again (at the targets less one) after a pause or on the first attack.
/// </summary>
[TestClass]
public sealed class ComboTest
{
    [TestMethod]
    public void TheComboGoesOnWithinTheWindowAndStartsAgainAfterIt()
    {
        var count = Combo.Next(0, long.MinValue, 1, 1_000);

        Assert.AreEqual(0, count, "the first attack of a run counts none");

        count = Combo.Next(count, 1_000, 2, 2_000);
        Assert.AreEqual(2, count, "an attack within the window adds its targets");

        count = Combo.Next(count, 2_000, 9, 4_999);
        Assert.AreEqual(7, count, "one attack counts at most five targets");

        count = Combo.Next(count, 4_999, 3, 7_999);
        Assert.AreEqual(2, count, "three seconds after the last hit the combo starts again");
    }
}
