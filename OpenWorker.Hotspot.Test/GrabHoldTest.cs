using System.Numerics;
using OpenWorker.Gameplay.Ai;
using OpenWorker.Gameplay.Combat;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The hold a monster has on a player after a blow that grabs.
/// </summary>
[TestClass]
public sealed class GrabHoldTest
{
    private static void AreClose(Vector3 expected, Vector3 actual)
    {
        Assert.IsLessThan(0.01f, Vector3.Distance(expected, actual), $"{actual} is not {expected}");
    }

    /// <summary>
    /// The place a player is put down is named from the monster: ahead of it is the way it looks, whichever way that is.
    /// </summary>
    [TestMethod]
    public void ThePlayerIsPutDownAheadOfTheMonsterWhicheverWayItLooks()
    {
        var ahead = new Vector3(0, -300, 40);

        AreClose(new Vector3(1000, 700, 40), GrabHold.DropPointOf(new Vector3(1000, 1000, 0), 0f, ahead, OpenNavigation.Instance, 35f));
        AreClose(new Vector3(1300, 1000, 40), GrabHold.DropPointOf(new Vector3(1000, 1000, 0), 90f, ahead, OpenNavigation.Instance, 35f));
    }

    /// <summary>
    /// A hold takes one player, begins to let go once, and the player has arrived when the time the animation gives is over.
    /// </summary>
    [TestMethod]
    public void AHoldLetsGoOnceAndThePlayerArrivesAfterTheTime()
    {
        var hold = new GrabHold();

        Assert.IsFalse(hold.IsHolding);
        Assert.IsFalse(hold.BeginDrop(1.0, 0.05f, null), "nobody is held");

        hold.Seize(7, new Vector3(1, 2, 3));

        Assert.IsTrue(hold.IsHolding);
        Assert.IsFalse(hold.HasArrived(5.0), "it has not been let go");
        Assert.IsTrue(hold.BeginDrop(1.0, 0.05f, null));
        Assert.IsFalse(hold.BeginDrop(1.01, 0.05f, null), "it is let go once");
        Assert.IsFalse(hold.HasArrived(1.04));
        Assert.IsTrue(hold.HasArrived(1.06));

        hold.Release();

        Assert.IsFalse(hold.IsHolding);
        Assert.IsFalse(hold.HasArrived(9.0));
    }
}
