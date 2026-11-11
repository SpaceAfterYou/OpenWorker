using System.Numerics;
using OpenWorker.Batch.Actions;
using OpenWorker.Gameplay.Combat;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A skill of a player that carries away the monsters before it.
/// </summary>
[TestClass]
public sealed class CarryRulesTest
{
    private static readonly AttachEvent Carry = new(0.1f, 0.3f, 100f, 100f, Vector3.Zero, 7);

    /// <summary>
    /// A monster before the player is carried, one behind it or to the side is not, and one beyond the distance of the event is
    /// not either.
    /// </summary>
    [TestMethod]
    public void OnlyWhatStandsBeforeThePlayerWithinTheDistanceIsTaken()
    {
        var area = new AttackArea(CarryRules.AreaOf(Carry, 170f), new Vector3(0, 0, 0), 0f);

        Assert.IsTrue(area.Touches(new Vector3(0, -80, 0), 30f, 170f), "before the player, which looks along -Y");
        Assert.IsFalse(area.Touches(new Vector3(0, 80, 0), 30f, 170f), "behind the player");
        Assert.IsFalse(area.Touches(new Vector3(0, -400, 0), 30f, 170f), "too far");
    }

    /// <summary>
    /// What weighs as much as the event allows, or stands fixed in its place, does not give: the player stops against it.
    /// </summary>
    [TestMethod]
    public void AHeavyOrFixedMonsterIsNotCarried()
    {
        Assert.IsFalse(CarryRules.IsTooHeavy(6, 0, 7));
        Assert.IsTrue(CarryRules.IsTooHeavy(7, 0, 7));
        Assert.IsTrue(CarryRules.IsTooHeavy(1, 2, 7));
        Assert.IsTrue(CarryRules.IsTooHeavy(1, 3, 7));
    }

    /// <summary>
    /// A carried monster keeps the way it stood before the player and its distance, a body of each added, wherever the player
    /// goes and turns; an event that names an offset puts it there instead.
    /// </summary>
    [TestMethod]
    public void ACarriedMonsterKeepsItsPlaceBeforeThePlayer()
    {
        var ahead = new Vector3(0, -1, 0);
        var first = CarryRules.Follow(new Vector3(100, 100, 0), 0f, 35f, 30f, ahead, 50f, Vector3.Zero);

        Assert.AreEqual(100f, first.X, 0.01f);
        Assert.AreEqual(100f - 115f, first.Y, 0.01f);

        var offset = CarryRules.Follow(new Vector3(100, 100, 0), 90f, 35f, 30f, ahead, 50f, new Vector3(0, -60, 0));

        Assert.AreEqual(160f, offset.X, 0.01f);
        Assert.AreEqual(100f, offset.Y, 0.01f);
    }
}
