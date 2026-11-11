using System.Numerics;
using OpenWorker.Batch.Actions;
using OpenWorker.Gameplay.Ai;

namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// The hold a monster has on a player after a blow that grabs. The player is taken by the first such blow that finds it, the
/// monster plays on without hurting anyone but the one it holds, and the hold ends when the animation lets the player go: the
/// player flies to the place the blow named, the blow that follows lands on it there, and it is put down. A hold also ends when
/// the skill of the monster is over or broken off, and when either of them dies.
/// </summary>
public sealed class GrabHold
{
    private double _dropEnd;

    /// <summary>
    /// The monster holds somebody, whether it has begun to let go or not.
    /// </summary>
    public bool IsHolding => Phase != GrabPhase.Free;

    public GrabPhase Phase { get; private set; }

    /// <summary>
    /// The actor that is held.
    /// </summary>
    public int Victim { get; private set; }

    /// <summary>
    /// Where the held player is put down.
    /// </summary>
    public Vector3 DropPoint { get; private set; }

    /// <summary>
    /// The blow that lands on the player when it arrives, if the animation names one.
    /// </summary>
    public AttackTrigger? FollowUp { get; private set; }

    /// <summary>
    /// Where the player is put down: the place of the blow turned by the way the monster looks, and no farther than the ground
    /// goes.
    /// </summary>
    public static Vector3 DropPointOf(Vector3 monster, float yaw, Vector3 offset, INavigation ground, float radius)
    {
        var turned = AiMath.Rotate(new Vector2(offset.X, offset.Y), yaw);
        var wanted = new Vector3(monster.X + turned.X, monster.Y + turned.Y, monster.Z + offset.Z);

        return ground.Clamp(monster, wanted, radius, out _);
    }

    public void Seize(int victim, Vector3 dropPoint)
    {
        Victim = victim;
        DropPoint = dropPoint;
        Phase = GrabPhase.Held;
        FollowUp = null;
    }

    /// <summary>
    /// The animation lets go: the player flies for <paramref name="dropTime"/> seconds. Nothing happens to a monster that holds
    /// nobody or has already begun to let go.
    /// </summary>
    public bool BeginDrop(double now, float dropTime, AttackTrigger? followUp)
    {
        if (Phase != GrabPhase.Held)
        {
            return false;
        }

        Phase = GrabPhase.Dropping;
        _dropEnd = now + Math.Max(dropTime, 0f);
        FollowUp = followUp;

        return true;
    }

    /// <summary>
    /// The player has arrived at the place it was thrown to.
    /// </summary>
    public bool HasArrived(double now) => Phase == GrabPhase.Dropping && now >= _dropEnd;

    public void Release()
    {
        Victim = 0;
        Phase = GrabPhase.Free;
        FollowUp = null;
    }
}

public enum GrabPhase
{
    Free,
    Held,
    Dropping
}
