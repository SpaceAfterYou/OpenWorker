using System.Numerics;
using OpenWorker.Batch.Actions;
using OpenWorker.Gameplay.Ai;

namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// A skill of a player that carries away what stands before it: for a short time, whatever is in front of the player and not
/// too heavy is taken along and moves as the player does, and a target that is too heavy stops the player instead. The
/// numbers are those of the event of the skill (<see cref="AttachEvent"/>).
/// </summary>
public static class CarryRules
{
    /// <summary>
    /// The space before the carrier in which a target is taken, as an area of an attack: a sector of the distance and the angle
    /// of the event, as high as a standing body.
    /// </summary>
    public static AttackTrigger AreaOf(AttachEvent carry, float height)
    {
        return new AttackTrigger(0, 0, 0, 0, 0f, carry.Angle, Vector3.Zero, 0f, 0f, carry.Distance, height, 0f);
    }

    /// <summary>
    /// A target of this rank or a higher one cannot be carried, nor can one that is fixed in its place.
    /// </summary>
    public static bool IsTooHeavy(int weightRank, int standType, int limit)
    {
        return weightRank >= limit || standType is 2 or 3;
    }

    /// <summary>
    /// Where a carried target stands: before the carrier, the way it stood when it was taken, a body of each and the distance
    /// it had apart from the two. An offset in the event puts it at that place from the carrier instead.
    /// </summary>
    public static Vector3 Follow(Vector3 carrier, float yaw, float carrierRadius, float radius, Vector3 direction, float distance, Vector3 offset)
    {
        if (offset != Vector3.Zero)
        {
            var turned = AiMath.Rotate(new Vector2(offset.X, offset.Y), yaw);

            return carrier + new Vector3(turned, offset.Z);
        }

        return carrier + direction * (carrierRadius + radius + distance);
    }
}
