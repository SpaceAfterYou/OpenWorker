using System.Numerics;
using OpenWorker.Batch.Actions;
using OpenWorker.Gameplay.Ai;
using OpenWorker.Gameplay.Combat;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// What a blow does to a monster over the time after it: where it is pushed, how long it is lost to the reaction, and when
/// it can act again.
/// </summary>
[TestClass]
public sealed class MonsterReactionTest
{
    private const float Tick = 0.05f;

    /// <summary>
    /// The lengths of the reaction animations of a small monster, in seconds.
    /// </summary>
    private static readonly Dictionary<string, float> Lengths = new()
    {
        ["B_DMG_L"] = 0.7f,
        ["B_DMG_R"] = 0.7f,
        ["B_KB_Start"] = 0.4f,
        ["B_KB_End"] = 0.5f,
        ["B_KD_Upp_Start"] = 0.4f,
        ["B_KD_Upp_End"] = 0.5f,
        ["B_KD_Upp_Down"] = 0.8f,
        ["B_KD_Upp_Raise"] = 0.9f
    };

    /// <summary>
    /// A floor with a wall across it.
    /// </summary>
    private sealed class WalledGround(float wall) : INavigation
    {
        public bool TryGetFloor(Vector3 position, out float height)
        {
            height = 0f;

            return true;
        }

        public bool TryFindPath(Vector3 from, Vector3 target, float radius, List<Vector3> path) => false;

        public bool CanSee(Vector3 from, Vector3 target) => true;

        public Vector3 Clamp(Vector3 from, Vector3 target, float radius, out bool reached)
        {
            reached = target.X <= wall - radius;

            return reached ? target : target with { X = wall - radius };
        }
    }

    private static MonsterReaction Create(INavigation? ground = null, int standType = 0, ReactionLimit limit = ReactionLimit.None)
    {
        return new MonsterReaction(new ReactionProfile(0, limit, standType, 0, 40f), ground ?? OpenNavigation.Instance, name => Lengths.GetValueOrDefault(name), new Random(1));
    }

    private static Blow Strike(ReactionKind kind, float distance, float height = 0f, float speed = 0f, short hardness = 1)
    {
        var push = new AttackReaction(distance, height, 0f, speed, AttackReaction.StraightAlongAttacker, 0f, 0f, 0f, 0f, 0f, 0, 0, false, hardness, false);

        return new Blow(push, kind, 1f, 90f, Vector3.Zero, Vector3.Zero, 0f, false);
    }

    /// <summary>
    /// Runs the reaction until it ends, and tells how long that took, what happened and where the monster ended.
    /// </summary>
    private static (float Seconds, ReactionEvents Events, Vector3 Position) RunOut(MonsterReaction reaction, Vector3 position)
    {
        var events = ReactionEvents.None;
        var seconds = 0f;

        while (reaction.IsBusy && seconds < 30f)
        {
            var step = reaction.Step(Tick, position);

            position = step.Position;
            events |= step.Events;
            seconds += Tick;
        }

        return (seconds, events, position);
    }

    /// <summary>
    /// A flinch costs the monster the length of its animation: it can neither attack nor walk until it is over, and a plain
    /// push slides it to the place the clients are told within the third of a second.
    /// </summary>
    [TestMethod]
    public void AFlinchLastsTheAnimationAndSlidesTheMonster()
    {
        var reaction = Create();
        var start = new Vector3(0, 0, 0);

        var outcome = reaction.Hit(Strike(ReactionKind.Flinch, 120f), DefenseType.Normal, true, start, new BlowRolls(0, 0));

        Assert.IsTrue(outcome.Shown);
        Assert.AreEqual(0f, outcome.FlySpeed);
        Assert.IsTrue(reaction.BlocksAttack && reaction.BlocksMove);
        Assert.AreEqual(new Vector3(0, -120, 0), outcome.ExtraMove, "The clients are told the place the slide ends at.");

        var (seconds, events, end) = RunOut(reaction, start);

        Assert.AreEqual(0.7f, seconds, Tick * 1.5f);
        Assert.IsTrue(events.HasFlag(ReactionEvents.Ended));
        Assert.AreEqual(-120f, end.Y, 4f);
        Assert.IsFalse(reaction.BlocksAttack || reaction.BlocksMove);
    }

    /// <summary>
    /// A launch throws the monster along the yaw of the blow and up, lets it fall, and holds it through the landing, the time
    /// it lies and the getting up: the monster is told to rise when it begins to, and is free only after.
    /// </summary>
    [TestMethod]
    public void ALaunchFliesLiesAndGetsUp()
    {
        var reaction = Create();
        var start = new Vector3(0, 0, 0);

        var outcome = reaction.Hit(Strike(ReactionKind.LaunchUp, 400f, height: 100f, speed: 1000f), DefenseType.Normal, true, start, new BlowRolls(0, 0));

        Assert.IsTrue(outcome.Shown);
        Assert.AreEqual(1000f, outcome.FlySpeed, "The clients fly the monster at the speed of the blow.");
        Assert.AreEqual(new Vector3(0, -400, 100), outcome.ExtraMove, "A throw is told in the frame of the attacker.");
        Assert.IsTrue(reaction.BlocksAttack, "Thrown, the monster cannot attack.");

        var (seconds, events, end) = RunOut(reaction, outcome.Position);

        // The air takes 2 * sqrt(2 * 100 * 1960) / 1960 = 0.64 s and a hang of 0.3 s; then the landing, the lying and the rising.
        var least = 0.64f + 0.3f + 0.5f + 1f + 0.9f;
        var most = least + 2f + (Tick * 3);

        Assert.IsTrue(seconds >= least - Tick && seconds <= most, $"The monster was lost to the throw for {seconds:F2} s.");
        Assert.IsTrue(events.HasFlag(ReactionEvents.Landed) && events.HasFlag(ReactionEvents.RaiseBegan) && events.HasFlag(ReactionEvents.Ended));
        Assert.AreEqual(0f, end.Z, 0.01f, "The monster is on the floor again.");
        Assert.AreEqual(-400f, end.Y, 20f, "It flew as far as the blow says.");
        Assert.AreEqual(0f, end.X, 0.5f);
    }

    /// <summary>
    /// A wall stops a flight a radius before it, and the monster falls where it is: the clients are told where.
    /// </summary>
    [TestMethod]
    public void AWallCutsAFlightShort()
    {
        var reaction = Create(new WalledGround(200f));
        var blow = Strike(ReactionKind.LaunchUp, 400f, height: 100f, speed: 1000f) with { Yaw = 90f };

        var outcome = reaction.Hit(blow, DefenseType.Normal, true, Vector3.Zero, new BlowRolls(0, 0));
        var (_, events, end) = RunOut(reaction, outcome.Position);

        Assert.IsTrue(events.HasFlag(ReactionEvents.FlightCut));
        Assert.AreEqual(160f, end.X, 5f, "A radius short of the wall.");
    }

    /// <summary>
    /// A monster whose armor holds shows no reaction and is not moved by a blow that is not hard enough to break it; a hard
    /// blow makes it flinch like any other.
    /// </summary>
    [TestMethod]
    public void AMonsterWithArmorIsMovedOnlyByAHardBlow()
    {
        var reaction = Create();

        var plain = reaction.Hit(Strike(ReactionKind.Flinch, 120f), DefenseType.SuperArmor, true, Vector3.Zero, new BlowRolls(0, 0));

        Assert.IsFalse(plain.Shown);
        Assert.AreEqual(Vector3.Zero, plain.ExtraMove);
        Assert.IsFalse(reaction.IsBusy);

        var hard = reaction.Hit(Strike(ReactionKind.Flinch, 120f, hardness: 3), DefenseType.SuperArmor, true, Vector3.Zero, new BlowRolls(0, 0));

        Assert.IsTrue(hard.Shown);
        Assert.IsTrue(reaction.IsBusy);
    }

    /// <summary>
    /// A weak blow shows no animation and loses the monster no time, though it may push; a monster fixed in its place flinches
    /// and slides nowhere, and attacks through the flinch.
    /// </summary>
    [TestMethod]
    public void AWeakBlowCostsNoTimeAndAFixedMonsterDoesNotSlide()
    {
        var reaction = Create();

        var weak = reaction.Hit(Strike(ReactionKind.Flinch, 120f, hardness: 0), DefenseType.Normal, true, Vector3.Zero, new BlowRolls(0, 0));

        Assert.IsFalse(weak.Shown);
        Assert.IsFalse(reaction.BlocksAttack);
        Assert.AreEqual(new Vector3(0, -120, 0), weak.ExtraMove, "The clients still slide it.");

        var fixedMonster = Create(standType: 2);
        var outcome = fixedMonster.Hit(Strike(ReactionKind.Flinch, 120f), DefenseType.Normal, true, Vector3.Zero, new BlowRolls(0, 0));

        Assert.IsTrue(outcome.Shown);
        Assert.AreEqual(Vector3.Zero, outcome.ExtraMove);
        Assert.IsFalse(fixedMonster.BlocksAttack, "A flinch does not keep it from attacking.");
        Assert.IsTrue(fixedMonster.BlocksMove);
    }

    /// <summary>
    /// A monster that dies of the blow is not moved, and one in the break of its armor takes nothing from a blow.
    /// </summary>
    [TestMethod]
    public void TheDeadAndTheMonsterInAnArmorBreakAreNotMoved()
    {
        var reaction = Create();

        reaction.Hit(Strike(ReactionKind.Flinch, 120f), DefenseType.Normal, false, Vector3.Zero, new BlowRolls(0, 0));

        Assert.IsFalse(reaction.IsBusy);

        var killed = reaction.Hit(Strike(ReactionKind.LaunchUp, 400f, height: 100f, speed: 1000f), DefenseType.Normal, false, Vector3.Zero, new BlowRolls(0, 0));

        Assert.IsFalse(reaction.IsBusy, "A monster that dies is not thrown.");
        Assert.AreEqual(1000f, killed.FlySpeed, "The clients fly it all the same.");

        reaction.BreakArmor(1f);

        var refused = reaction.Hit(Strike(ReactionKind.LaunchUp, 400f, height: 100f), DefenseType.Normal, true, Vector3.Zero, new BlowRolls(0, 0));

        Assert.IsFalse(refused.Shown);
        Assert.IsTrue(reaction.IsArmorBreaking);

        var (seconds, _, _) = RunOut(reaction, Vector3.Zero);

        Assert.AreEqual(1f, seconds, Tick * 1.5f);
    }
}
