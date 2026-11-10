using System.Numerics;
using OpenWorker.Batch.Actions;
using OpenWorker.Gameplay.Combat;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// How far, how high and which way a blow throws what it hits: one rule for the blows of players and of monsters.
/// </summary>
[TestClass]
public sealed class ExtraMoveTest
{
    private static readonly BlowRolls NoRolls = new(0, 0);

    private static AttackReaction Push(float distance = 100f, float height = 0f, int arrow = AttackReaction.AlongAttacker, short hardness = 1, bool weight = false)
    {
        return new AttackReaction(distance, height, 30f, 0f, arrow, 0f, 0f, 0f, 0f, 0f, 0, 0, weight, hardness, false);
    }

    private static Blow Strike(AttackReaction push, ReactionKind kind, float yaw = 0f, int weight = 0, float charging = 0f, float damage = 1f)
    {
        return new Blow(push, kind, damage, 90f, Vector3.Zero, Vector3.Zero, yaw, false, weight, charging);
    }

    private static Struck Target(int weight = 0, ReactionLimit limit = ReactionLimit.None, DefenseType defense = DefenseType.Normal, bool airborne = false, bool held = false)
    {
        return new Struck(new Vector3(0, -100, 0), weight, limit, 0, defense, airborne, held);
    }

    private static void AreClose(Vector3 expected, Vector3 actual)
    {
        Assert.IsLessThan(0.01f, Vector3.Distance(expected, actual), $"{actual} is not {expected}");
    }

    /// <summary>
    /// A throw that lifts the target is told in the frame of the attacker, which the client turns by the yaw of the blow; a
    /// flinch is told as the push in the world, turned here. The straight arrow takes no roll of the dice.
    /// </summary>
    [TestMethod]
    public void ALaunchIsToldInTheFrameOfTheAttackerAndAFlinchInTheWorld()
    {
        var rolls = new BlowRolls(10, 0);

        var launch = ExtraMove.Of(Strike(Push(height: 80f), ReactionKind.LaunchUp, yaw: 90f), Target(), rolls);

        AreClose(new Vector3(0, -110, 80), launch.Vector);
        Assert.AreEqual(90f, launch.Yaw);

        var flinch = ExtraMove.Of(Strike(Push(), ReactionKind.Flinch, yaw: 90f), Target(), rolls);

        AreClose(new Vector3(110, 0, 0), flinch.Vector);

        var straight = ExtraMove.Of(Strike(Push(arrow: AttackReaction.StraightAlongAttacker), ReactionKind.Flinch, yaw: 90f), Target(), rolls);

        AreClose(new Vector3(100, 0, 0), straight.Vector);
    }

    /// <summary>
    /// A target that weighs two ranks or more less than the attacker flies a third farther, one that weighs two or three
    /// more flies a third less far, and one that weighs four more does not fly. Only the throws that lift count the weight.
    /// </summary>
    [TestMethod]
    public void AHeavyAttackerThrowsFartherAndALightOneNotAtAll()
    {
        var push = Push(height: 80f, weight: true);

        Assert.AreEqual(-135f, ExtraMove.Of(Strike(push, ReactionKind.KnockDown, weight: 3), Target(weight: 1), NoRolls).Vector.Y, 0.01f);
        Assert.AreEqual(-100f, ExtraMove.Of(Strike(push, ReactionKind.KnockDown, weight: 1), Target(weight: 1), NoRolls).Vector.Y, 0.01f);
        Assert.AreEqual(-65f, ExtraMove.Of(Strike(push, ReactionKind.KnockDown, weight: 1), Target(weight: 3), NoRolls).Vector.Y, 0.01f);
        Assert.AreEqual(Vector3.Zero, ExtraMove.Of(Strike(push, ReactionKind.KnockDown, weight: 1), Target(weight: 5), NoRolls).Vector);
        Assert.AreEqual(-100f, ExtraMove.Of(Strike(push, ReactionKind.Flinch, weight: 1), Target(weight: 5), NoRolls).Vector.Y, 0.01f, "A flinch has no weight.");
    }

    /// <summary>
    /// Nobody is thrown who cannot be, who is held by the attacker, by a blow that deals nothing or by one that reaches
    /// nobody: weak, with no distance and no height.
    /// </summary>
    [TestMethod]
    public void NothingMovesATargetThatCannotBeThrown()
    {
        var push = Push(height: 80f);

        Assert.AreEqual(Vector3.Zero, ExtraMove.Of(Strike(push, ReactionKind.LaunchUp), Target(limit: ReactionLimit.Immovable), NoRolls).Vector);
        Assert.AreEqual(Vector3.Zero, ExtraMove.Of(Strike(push, ReactionKind.LaunchUp), Target(held: true), NoRolls).Vector);
        Assert.AreEqual(Vector3.Zero, ExtraMove.Of(Strike(push, ReactionKind.LaunchUp, damage: 0f), Target(), NoRolls).Vector);
        Assert.AreEqual(Vector3.Zero, ExtraMove.Of(Strike(Push(distance: 0f, hardness: 0), ReactionKind.Flinch), Target(), NoRolls).Vector);
        Assert.AreNotEqual(Vector3.Zero, ExtraMove.Of(Strike(push, ReactionKind.LaunchUp), Target(limit: ReactionLimit.NoLaunch), NoRolls).Vector, "A limit changes the reaction, not the throw.");
    }

    /// <summary>
    /// A guard takes half the push, a charge adds to it, and a target in the air rises by the height meant for the air.
    /// </summary>
    [TestMethod]
    public void AGuardTakesHalfAChargeAddsAndTheAirHasItsOwnHeight()
    {
        Assert.AreEqual(-50f, ExtraMove.Of(Strike(Push(height: 80f), ReactionKind.LaunchUp), Target(defense: DefenseType.Guard), NoRolls).Vector.Y, 0.01f);
        Assert.AreEqual(-150f, ExtraMove.Of(Strike(Push(height: 80f), ReactionKind.LaunchUp, charging: 0.5f), Target(), NoRolls).Vector.Y, 0.01f);
        Assert.AreEqual(30f, ExtraMove.Of(Strike(Push(height: 80f), ReactionKind.LaunchUp), Target(airborne: true), NoRolls).Vector.Z, 0.01f);
    }

    /// <summary>
    /// A blow that comes from a place pushes away from it: a target east of the burst of a shot goes east, whichever way the
    /// attacker looks.
    /// </summary>
    [TestMethod]
    public void AShotPushesAwayFromWhereItBurst()
    {
        var blow = Strike(Push(arrow: AttackReaction.AwayFromOrigin), ReactionKind.Flinch, yaw: 180f) with { IsMelee = false, Origin = new Vector3(0, 0, 0) };
        var east = new Struck(new Vector3(500, 0, 0), 0, ReactionLimit.None, 0, DefenseType.Normal, false);

        var result = ExtraMove.Of(blow, east, NoRolls);

        AreClose(new Vector3(100, 0, 0), result.Vector);
        Assert.AreEqual(90f, result.Yaw, 0.01f, "The target flies the way it was pushed.");
    }
}
