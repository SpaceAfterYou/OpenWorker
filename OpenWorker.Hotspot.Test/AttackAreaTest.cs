using System.Numerics;
using OpenWorker.Batch.Actions;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class AttackAreaTest
{
    private const float Radius = 50f;
    private const float Height = 200f;

    // A narrow slice that starts behind the attacker and reaches 200 forward.
    private static readonly AttackTrigger Arc = new(0, 0, 0, 0, 0.25f, 40f, new Vector3(0, 250, 0), 100, 100, 450, 200, 200);
    private static readonly AttackTrigger Box = new(0, AttackTrigger.BoxRange, 0, 0, 0f, 0f, new Vector3(0, -200, 0), 100, 400, 0, 200, 0);

    private static bool Touches(in AttackTrigger trigger, float yaw, float x, float y, float z = 0)
    {
        return new AttackArea(trigger, Vector3.Zero, yaw).Touches(new Vector3(x, y, z), Radius, Height);
    }

    [TestMethod]
    public void ZeroYawLooksAlongNegativeY()
    {
        Assert.IsTrue(Touches(Arc, 0, 0, -150));
        Assert.IsFalse(Touches(Arc, 0, 0, 150));
    }

    [TestMethod]
    public void PositiveYawTurnsTowardsPositiveX()
    {
        Assert.IsTrue(Touches(Arc, 90, 150, 0));
        Assert.IsFalse(Touches(Arc, 90, -150, 0));
        Assert.IsTrue(Touches(Arc, -90, -150, 0));
        Assert.IsTrue(Touches(Arc, 180, 0, 150));
    }

    [TestMethod]
    public void ArcStopsAtItsEnds()
    {
        Assert.IsTrue(Touches(Arc, 0, 0, -240));
        Assert.IsFalse(Touches(Arc, 0, 0, -260));
        Assert.IsFalse(Touches(Arc, 0, 0, 110));
    }

    [TestMethod]
    public void ArcStopsAtItsSides()
    {
        // At this distance the slice is about 180 wide.
        Assert.IsTrue(Touches(Arc, 0, 120, -0));
        Assert.IsFalse(Touches(Arc, 0, 200, 0));
    }

    [TestMethod]
    public void FullCircleIgnoresDirection()
    {
        var circle = Arc with { Angle = 360f, Center = Vector3.Zero, StartPosition = 0 };

        Assert.IsTrue(Touches(circle, 0, 0, 400));
        Assert.IsTrue(Touches(circle, 0, 0, -400));
    }

    [TestMethod]
    public void HeightLimitsBothWays()
    {
        Assert.IsTrue(Touches(Arc, 0, 0, -150, 200));
        Assert.IsFalse(Touches(Arc, 0, 0, -150, 201));
        Assert.IsTrue(Touches(Arc, 0, 0, -150, -200));
        Assert.IsFalse(Touches(Arc, 0, 0, -150, -201));
    }

    [TestMethod]
    public void BoxTurnsWithTheAttacker()
    {
        Assert.IsTrue(Touches(Box, 0, 0, -350));
        Assert.IsFalse(Touches(Box, 0, 200, -200));
        Assert.IsTrue(Touches(Box, 90, 350, 0));
        Assert.IsFalse(Touches(Box, 90, 0, -350));
    }

    [TestMethod]
    public void BoxCountsTheBodyOfTheTarget()
    {
        Assert.IsTrue(Touches(Box, 0, 90, -200));
        Assert.IsFalse(Touches(Box, 0, 110, -200));
    }
}
