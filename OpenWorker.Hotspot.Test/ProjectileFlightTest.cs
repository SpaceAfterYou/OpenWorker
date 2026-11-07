using System.Numerics;
using OpenWorker.Batch.Actions;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class ProjectileFlightTest
{
    private const float Radius = 50f;
    private const float Height = 200f;

    private static readonly ProjectileInfo Straight = new(Vector3.Zero, 1000f, 1f, false, 1, 50f, false, 0f, false, false, 0, Vector3.Zero, false, 0);

    private static AttackTrigger Trigger(ProjectileInfo info, short type = AttackTrigger.FromBody)
    {
        return new AttackTrigger(14, 0, type, 0, 0f, 360f, Vector3.Zero, 0, 0, 100, 200, 0, 1f, 1, info);
    }

    private static ProjectileFlight Launch(ProjectileInfo info, float yaw = 0f, short type = AttackTrigger.FromBody, Vector3 camera = default, int? target = null)
    {
        return ProjectileFlight.Launch(Trigger(info, type), Vector3.Zero, Vector3.UnitX, yaw, camera, target);
    }

    private static FlightTarget Target(int key, float x, float y, float z = 0f)
    {
        return new FlightTarget(key, new Vector3(x, y, z), Radius, Height);
    }

    private static void AssertNear(Vector3 expected, Vector3 actual)
    {
        Assert.IsLessThan(0.01f, Vector3.Distance(expected, actual), $"{expected} and {actual}");
    }

    [TestMethod]
    public void FliesWhereTheShooterLooks()
    {
        AssertNear(new Vector3(0, -1, 0), Launch(Straight).Direction);
        AssertNear(new Vector3(1, 0, 0), Launch(Straight, 90f).Direction);
        AssertNear(new Vector3(0, 1, 0), Launch(Straight, 180f).Direction);
    }

    [TestMethod]
    public void TemplateTurnsAndLowers()
    {
        var turned = Launch(Straight with { Direction = new Vector3(90f, 0, 0) });

        AssertNear(new Vector3(1, 0, 0), turned.Direction);

        var lowered = Launch(Straight with { Direction = new Vector3(0, 90f, 0) });

        AssertNear(new Vector3(0, 0, -1), lowered.Direction);
    }

    [TestMethod]
    public void CameraDecidesWhenAsked()
    {
        var byCamera = Straight with { DirectionType = ProjectileInfo.ByCamera };

        AssertNear(new Vector3(0, -1, 0), Launch(byCamera, 77f, camera: new Vector3(180f, 0, 0)).Direction);
        AssertNear(new Vector3(0, 1, 0), Launch(byCamera, 77f, camera: new Vector3(0f, 0, 0)).Direction);
    }

    [TestMethod]
    public void StartsAtTheHandOfTheShooter()
    {
        var hand = Straight with { Start = new Vector3(50, -15, 110) };

        AssertNear(new Vector3(-15, -50, 110), Launch(hand).Position);
        AssertNear(new Vector3(15, -50, 110), ProjectileFlight.Launch(Trigger(hand, 1), Vector3.Zero, new Vector3(-1, 0, 0), 0f, default).Position);
    }

    [TestMethod]
    public void HitsWhatIsOnItsWayAndStops()
    {
        var flight = Launch(Straight);
        var explosions = flight.Advance(0.5f, [Target(1, 0, -300), Target(2, 0, -400)]);

        Assert.HasCount(1, explosions);
        Assert.AreEqual(1, explosions[0].Collider);
        Assert.IsTrue(flight.IsOver);
    }

    [TestMethod]
    public void FastProjectileDoesNotJumpOverATarget()
    {
        var fast = Straight with { Speed = 20000f, Radius = 10f, LifeTime = 1f };

        Assert.HasCount(1, Launch(fast).Advance(0.05f, [Target(1, 0, -500)]));
    }

    [TestMethod]
    public void MissesWhatIsAside()
    {
        var flight = Launch(Straight);

        Assert.IsEmpty(flight.Advance(0.5f, [Target(1, 300, -300), Target(2, 0, 300)]));
        Assert.IsFalse(flight.IsOver);
    }

    [TestMethod]
    public void EndsWithItsLife()
    {
        var flight = Launch(Straight);

        Assert.IsEmpty(flight.Advance(1.1f, [Target(1, 0, -5000)]));
        Assert.IsTrue(flight.IsOver);
    }

    [TestMethod]
    public void GoesThroughAsManyAsAllowed()
    {
        var piercing = Straight with { Penetrates = true, PenetrateCount = 1 };
        var flight = Launch(piercing);
        var explosions = flight.Advance(0.9f, [Target(1, 0, -200), Target(2, 0, -400), Target(3, 0, -600)]);

        CollectionAssert.AreEqual(new int?[] { 1, 2 }, explosions.Select(e => e.Collider).ToArray());
        Assert.IsTrue(flight.IsOver);
    }

    [TestMethod]
    public void DoesNotHitTheSameTargetTwiceWhenGoingThrough()
    {
        var piercing = Straight with { Penetrates = true, PenetrateCount = 9, Speed = 100f };
        var flight = Launch(piercing);
        var target = Target(1, 0, -10);

        Assert.HasCount(1, flight.Advance(0.05f, [target]));
        Assert.IsEmpty(flight.Advance(0.05f, [target]));
    }

    [TestMethod]
    public void BombWaitsForTheGround()
    {
        var bomb = Straight with { Start = new Vector3(0, 0, 100), ExplodeDecision = 6, Direction = new Vector3(0, 90f, 0) };
        var flight = Launch(bomb);
        var explosions = flight.Advance(0.2f, [Target(1, 0, 0, 50)]);

        Assert.HasCount(1, explosions);
        Assert.IsNull(explosions[0].Collider);
        Assert.AreEqual(0f, explosions[0].Position.Z);
    }

    [TestMethod]
    public void GravityBringsItDown()
    {
        var heavy = Straight with { HasGravity = true, Start = new Vector3(0, 0, 100), LifeTime = 5f };
        var flight = Launch(heavy);

        flight.Advance(0.1f, []);

        Assert.IsLessThan(100f, flight.Position.Z);
    }

    [TestMethod]
    public void AlongGroundStaysLow()
    {
        var low = Straight with { DirectionType = ProjectileInfo.AlongGround, Direction = new Vector3(0, 30f, 0) };
        var flight = Launch(low);

        flight.Advance(0.1f, []);

        Assert.AreEqual(0f, flight.Position.Z);
    }

    [TestMethod]
    public void ContinuousHitsOnItsOwnSchedule()
    {
        var lingering = Straight with { Speed = 0f, IsContinuous = true, ContinuousTime = 0.2f, LifeTime = 1f };
        var flight = Launch(lingering);
        var count = 0;

        for (var i = 0; i < 10; i++)
        {
            count += flight.Advance(0.05f, []).Count;
        }

        Assert.AreEqual(2, count);
    }

    [TestMethod]
    public void GuidedFollowsItsTarget()
    {
        var guided = Straight with { IsGuided = true };
        var flight = Launch(guided, target: 5);
        var explosions = flight.Advance(0.5f, [Target(5, 300, 0)]);

        Assert.AreEqual(5, explosions.Single().Collider);
    }

    [TestMethod]
    public void NoHitsWithoutTheRightDecision()
    {
        var harmless = Straight with { ExplodeDecision = 3 };

        Assert.IsEmpty(Launch(harmless).Advance(0.5f, [Target(1, 0, -100)]));
    }
}
