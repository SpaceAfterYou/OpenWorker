using OpenWorker.Batch.Actions;

namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// How a character takes hits at a moment of the animation it plays. Its own defense (what its table gives, with the buffs) is
/// the one it has when the animation says nothing; an animation can make it armored, invincible, plain, or immune to reactions
/// for as long as it lasts, and a stretch of the animation can do the same for a time. This is how a hero is armored while it
/// uses a skill and cannot be hit while it rolls.
/// </summary>
public static class DefenseFrames
{
    /// <summary>
    /// The defense type at <paramref name="elapsed"/> seconds into an animation.
    /// </summary>
    /// <param name="own">The defense the character has when the animation says nothing.</param>
    /// <param name="animation">What the animation says, see <see cref="ActionDefense"/>.</param>
    /// <param name="stretches">The stretches of the animation that say something else.</param>
    /// <param name="elapsed">Seconds from the start of the animation.</param>
    /// <param name="armorBroken">The armor of the character is broken: the stretches of the animation say nothing then.</param>
    public static DefenseType Of(DefenseType own, byte animation, IReadOnlyList<DefenseEvent>? stretches, float elapsed, bool armorBroken = false)
    {
        var stretch = armorBroken ? null : StretchAt(stretches, elapsed);

        // Whoever cannot be hurt, or is armored, is so whatever else the animation says.
        if (animation == ActionDefense.Invincible || stretch == ActionDefense.Invincible)
        {
            return DefenseType.Invincible;
        }

        if (animation == ActionDefense.SuperArmor || stretch == ActionDefense.SuperArmor)
        {
            return DefenseType.SuperArmor;
        }

        if (stretch == ActionDefense.Normal)
        {
            return DefenseType.Normal;
        }

        return animation switch
        {
            ActionDefense.Normal => DefenseType.Normal,
            (byte)DefenseType.Guard => DefenseType.Guard,
            (byte)DefenseType.NoReaction => DefenseType.NoReaction,
            _ => own
        };
    }

    /// <summary>
    /// What the stretch that has begun last and has not ended says, if any.
    /// </summary>
    private static byte? StretchAt(IReadOnlyList<DefenseEvent>? stretches, float elapsed)
    {
        byte? found = null;

        foreach (var stretch in stretches ?? [])
        {
            // A stretch that ends before it begins lasts the rest of the animation.
            if (elapsed >= stretch.Start && (stretch.End <= stretch.Start || elapsed < stretch.End))
            {
                found = stretch.Defense;
            }
        }

        return found;
    }
}
