using System.Numerics;
using OpenWorker.Batch.Actions;
using OpenWorker.Gameplay.Ai;
using OpenWorker.Gameplay.Combat;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A monster that escapes a run of blows stops flinching at once, but a monster thrown into the air keeps flying.
/// </summary>
[TestClass]
public sealed class MonsterFlinchEndTest
{
    private static MonsterReaction Hit(ReactionKind kind, float distance, float height)
    {
        var reaction = new MonsterReaction(new ReactionProfile(0, ReactionLimit.None, 0, 0, 40f), OpenNavigation.Instance, _ => 0.7f, new Random(1));
        var push = new AttackReaction(distance, height, 0f, 1000f, AttackReaction.StraightAlongAttacker, 0f, 0f, 0f, 0f, 0f, 0, 0, false, 1, false);

        reaction.Hit(new Blow(push, kind, 1f, 90f, Vector3.Zero, Vector3.Zero, 0f, false), DefenseType.Normal, true, Vector3.Zero, new BlowRolls(0, 0));

        return reaction;
    }

    [TestMethod]
    public void AFlinchEndsAtOnceWhenAskedAndAThrowDoesNot()
    {
        var flinch = Hit(ReactionKind.Flinch, 100f, 0f);

        Assert.IsTrue(flinch.IsBusy);

        flinch.EndFlinch();

        Assert.IsFalse(flinch.IsBusy);

        var thrown = Hit(ReactionKind.LaunchUp, 400f, 100f);

        thrown.EndFlinch();

        Assert.IsTrue(thrown.IsBusy);
    }
}
