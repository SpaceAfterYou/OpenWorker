using System.Numerics;
using OpenWorker.Batch.Actions;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class ChainLightningTest
{
    private const float Radius = 50f;
    private const float Height = 200f;
    private const float Step = 0.05f;

    private static AttackTrigger Trigger(int connection, int max = 5, bool continuous = false, bool recalculates = false, float radius = 300f)
    {
        var flight = new ProjectileInfo(Vector3.Zero, 3500f, 1f, false, 1, 10f, continuous, 0.2f, false, false, 0, Vector3.Zero, false, 0);
        var chain = new ChainInfo(connection, 3f, max, 0.5f, Vector3.Zero, radius, 0f, recalculates);

        // In front of the attacker, up to 1000 away.
        return new AttackTrigger(105, 0, 5, 0, 0f, 90f, Vector3.Zero, 0, 0, 1000, 200, 0, 1f, 1, flight, chain);
    }

    private static FlightTarget Target(int key, float x, float y)
    {
        return new FlightTarget(key, new Vector3(x, y, 0), Radius, Height);
    }

    private static (List<ChainHit> Hits, List<ChainChange> Changes) Run(ChainLightning chain, IReadOnlyList<FlightTarget> targets, float seconds)
    {
        var hits = new List<ChainHit>();
        var changes = new List<ChainChange>();

        for (var time = 0f; time < seconds; time += Step)
        {
            chain.Advance(Step, targets, Vector3.Zero, 0f, hits, changes);
        }

        return (hits, changes);
    }

    // A line of targets in front of the attacker, 250 apart, and one behind it.
    private static readonly FlightTarget[] Line = [Target(1, 0, -400), Target(2, 0, -650), Target(3, 0, -900), Target(4, 0, -1150), Target(9, 0, 500)];

    [TestMethod]
    public void StartsAtTheNearestTargetInFront()
    {
        var chain = ChainLightning.Fire(Trigger(ChainInfo.OneByOne), Vector3.Zero, 0f, Line);

        CollectionAssert.AreEqual(new[] { 1 }, chain.Targets.ToArray());
    }

    [TestMethod]
    public void WithoutAnyoneInFrontHoldsNobody()
    {
        var chain = ChainLightning.Fire(Trigger(ChainInfo.OneByOne), Vector3.Zero, 0f, [Target(9, 0, 500)]);

        Assert.IsEmpty(chain.Targets);
        Assert.IsEmpty(Run(chain, [Target(9, 0, 500)], 1f).Hits);
    }

    [TestMethod]
    public void OneByOneJumpsFromTheLastTarget()
    {
        var chain = ChainLightning.Fire(Trigger(ChainInfo.OneByOne), Vector3.Zero, 0f, Line);
        var (hits, changes) = Run(chain, Line, 1f);

        CollectionAssert.AreEqual(new[] { new ChainHit(1, 0), new ChainHit(2, 1), new ChainHit(3, 2), new ChainHit(4, 3) }, hits);
        CollectionAssert.AreEqual(new[] { new ChainChange(2, true), new ChainChange(3, true), new ChainChange(4, true) }, changes);
    }

    [TestMethod]
    public void StopsAtTheLimit()
    {
        var chain = ChainLightning.Fire(Trigger(ChainInfo.OneByOne, max: 2), Vector3.Zero, 0f, Line);

        Assert.HasCount(2, Run(chain, Line, 1f).Hits);
    }

    [TestMethod]
    public void FromFirstReachesOnlyThoseNearTheFirst()
    {
        var chain = ChainLightning.Fire(Trigger(ChainInfo.FromFirst), Vector3.Zero, 0f, Line);
        var (hits, _) = Run(chain, Line, 1f);

        // Only the second target is close enough to the first one.
        CollectionAssert.AreEqual(new[] { new ChainHit(1, 0), new ChainHit(2, 1) }, hits);
    }

    [TestMethod]
    public void AllAtOnceTakesEveryoneAroundTheAttacker()
    {
        var around = new[] { Target(1, 0, -200), Target(2, 150, -150), Target(3, -150, -150), Target(4, 0, -2000) };
        var chain = ChainLightning.Fire(Trigger(ChainInfo.AllAtOnce), Vector3.Zero, 0f, around);
        var (hits, changes) = Run(chain, around, 1f);

        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, hits.Select(e => e.Target).ToArray());
        Assert.HasCount(2, changes);
    }

    [TestMethod]
    public void RepeatingChainStrikesEveryoneAgain()
    {
        var two = new[] { Target(1, 0, -400), Target(2, 0, -650) };
        var chain = ChainLightning.Fire(Trigger(ChainInfo.OneByOne, continuous: true), Vector3.Zero, 0f, two);
        var (hits, _) = Run(chain, two, 1f);

        // Struck on arrival and then every 0.2 seconds.
        Assert.IsGreaterThanOrEqualTo(5, hits.Count(e => e.Target == 1), string.Join(", ", hits));
        Assert.IsTrue(hits.Where(e => e.Target == 2).All(e => e.Order == 1));
    }

    [TestMethod]
    public void RecalculatingChainKeepsItsTargetsWhileTheFirstStays()
    {
        var two = new[] { Target(1, 0, -400), Target(2, 0, -650) };
        var chain = ChainLightning.Fire(Trigger(ChainInfo.OneByOne, continuous: true, recalculates: true), Vector3.Zero, 0f, two);
        var (hits, changes) = Run(chain, two, 1f);

        Assert.IsGreaterThanOrEqualTo(5, hits.Count(e => e.Target == 1), string.Join(", ", hits));
        Assert.IsFalse(changes.Any(e => !e.Added), string.Join(", ", changes));
    }

    [TestMethod]
    public void EndsWhenItsLifeIsOver()
    {
        var chain = ChainLightning.Fire(Trigger(ChainInfo.OneByOne, continuous: true), Vector3.Zero, 0f, Line);

        Run(chain, Line, 3.1f);

        Assert.IsTrue(chain.IsOver);
        Assert.IsEmpty(Run(chain, Line, 1f).Hits);
    }

    [TestMethod]
    public void TargetThatIsGoneLeavesTheChain()
    {
        var chain = ChainLightning.Fire(Trigger(ChainInfo.OneByOne, continuous: true), Vector3.Zero, 0f, Line);

        Run(chain, Line, 0.5f);

        var (hits, changes) = Run(chain, Line[1..], 0.5f);

        Assert.IsFalse(hits.Any(e => e.Target == 1));
        Assert.Contains(new ChainChange(1, false), changes);
    }
}
