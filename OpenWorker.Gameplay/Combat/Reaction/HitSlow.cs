using OpenWorker.Batch.Actions;

namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// A blow slows what it hits for a moment. A strong blow does it silently, every client reads it from the event of the attack;
/// a weak blow does it by chance, and the clients are told when it did.
/// </summary>
public static class HitSlow
{
    private const float WeakShare = 0.1f;
    private const int Chances = 100;

    /// <summary>
    /// How long a weak blow slows its target, in seconds, if it does: nothing for a target that has a defense type, or when
    /// the chance of the event is not rolled. A strong blow is not told, so it gives nothing here.
    /// </summary>
    /// <param name="roll">A number from 0 to 99.</param>
    public static float TimeToTell(in AttackReaction push, DefenseType defense, int roll)
    {
        if (push.SlowTime <= 0f || push.Hardness != 0 || defense != DefenseType.Normal || roll > push.WeakSlowChance)
        {
            return 0f;
        }

        return push.SlowTime * WeakShare;
    }

    /// <summary>
    /// A number from 0 to 99 for <see cref="TimeToTell"/>.
    /// </summary>
    public static int Roll(Random random) => random.Next(Chances);
}
