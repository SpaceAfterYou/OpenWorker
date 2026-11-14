using System.Numerics;
using OpenWorker.Gameplay.Ai;

namespace OpenWorker.Hotspot.Test;

public sealed partial class AiMachineTest
{
    /// <summary>
    /// Ground with a wall halfway between any two places: a creature pushed from one towards the other stops at the middle.
    /// </summary>
    private sealed class HalfwayWall(bool floor) : INavigation
    {
        public bool TryGetFloor(Vector3 position, out float height)
        {
            height = position.Z;

            return floor;
        }

        public bool TryFindPath(Vector3 from, Vector3 target, float radius, List<Vector3> path) => false;

        public bool CanSee(Vector3 from, Vector3 target) => true;

        public Vector3 Clamp(Vector3 from, Vector3 target, float radius, out bool reached)
        {
            reached = false;

            return (from + target) / 2f;
        }
    }

    /// <summary>
    /// Two monsters that come at one target from the same side do not end on one spot: each takes its own place around it.
    /// </summary>
    [TestMethod]
    public void MonstersComingAtOneTargetFromOneSideTakeTheirOwnPlaces()
    {
        var places = new TargetPlaces();
        var first = new Host { Player = Player(Vector3.Zero), Position = new Vector3(900, 0, 0), Places = places };
        var second = new Host { Player = Player(Vector3.Zero), Position = new Vector3(900, 10, 0), Places = places };

        first.Skills[0] = Skill(11, max: 150);
        second.Skills[0] = Skill(11, max: 150);

        var machines = new[] { (Create(Fighter(), first), first), (Create(Fighter(), second, 2), second) };

        foreach (var (machine, _) in machines)
        {
            machine.Start();
        }

        for (var elapsed = 0f; elapsed < 4f; elapsed += Step)
        {
            foreach (var (machine, host) in machines)
            {
                host.Advance(Step);
                machine.Update(Step);
            }
        }

        Assert.IsNotEmpty(first.Used);
        Assert.IsNotEmpty(second.Used);
        Assert.AreNotEqual(machines[0].Item1.Place, machines[1].Item1.Place);
        Assert.IsGreaterThan(50f, Vector3.Distance(first.Position, second.Position));
    }

    /// <summary>
    /// The approach looks again every tenth of a second, but looks for a new way only when the target has moved.
    /// </summary>
    [TestMethod]
    public void TheApproachLooksForANewWayOnlyWhenTheTargetMoves()
    {
        var host = new Host { Player = Player(Vector3.Zero), Position = new Vector3(900, 0, 0), Speed = 100f };

        host.Skills[0] = Skill(11, max: 150);

        var machine = Create(Fighter(), host);

        machine.Start();
        Run(machine, host, 2f);

        Assert.IsTrue(host.IsMoving);
        Assert.AreEqual(1, host.Searches);

        host.Player = Player(new Vector3(0, 300, 0));

        Run(machine, host, 0.25f);

        Assert.AreEqual(2, host.Searches);
    }

    /// <summary>
    /// The third way in a row not found to its target puts the monster as near itself as the target could go in a straight
    /// line, and tells the clients.
    /// </summary>
    [TestMethod]
    public void AMonsterThatFindsNoWayThreeTimesIsPutWhereItsTargetCouldGo()
    {
        var host = new Host { Player = Player(Vector3.Zero), Position = new Vector3(900, 0, 0), NoWay = true, Navigation = new HalfwayWall(true) };

        host.Skills[0] = Skill(11, max: 150);

        var machine = Create(Fighter(), host);

        machine.Start();
        Run(machine, host, 1f);

        Assert.DoesNotContain("teleport", host.Log, "Two ways not found yet.");

        for (var elapsed = 0f; elapsed < 5f && !host.Log.Contains("teleport"); elapsed += Step)
        {
            Run(machine, host, Step);
        }

        Assert.Contains("teleport", host.Log);
        Assert.AreEqual(new Vector3(450, 0, 0), host.Position);
    }

    /// <summary>
    /// A monster that waits where there is no floor is put home after five seconds of it.
    /// </summary>
    [TestMethod]
    public void AMonsterWaitingWithoutFloorIsPutHomeAfterFiveSeconds()
    {
        var host = new Host { Position = new Vector3(500, 0, 0), HomePosition = Vector3.Zero, Navigation = new HalfwayWall(false) };
        var machine = Create(Fighter(), host);

        machine.Start();
        Run(machine, host, 4.5f);

        Assert.DoesNotContain("teleport", host.Log);

        Run(machine, host, 1f);

        Assert.AreEqual(1, host.Log.Count(e => e == "teleport"));
        Assert.AreEqual(Vector3.Zero, host.Position);
    }
}
