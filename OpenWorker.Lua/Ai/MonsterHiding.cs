using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Modules.Monster.Responses;

namespace OpenWorker.Lua.Ai;

/// <summary>
/// A monster gone from sight: what it keeps while it is gone, and when it shows itself again.
/// </summary>
internal sealed class MonsterHiding
{
    /// <summary>
    /// The monster can still be hit.
    /// </summary>
    public const byte KeepsCollision = 1;

    /// <summary>
    /// The monster can still walk.
    /// </summary>
    public const byte KeepsMovement = 4;

    /// <summary>
    /// How often a monster that hides until others die looks whether they have, in seconds.
    /// </summary>
    private const double Look = 1.0;

    // The time a hiding for a time has left, in seconds of the monster's animation.
    private double _left;
    private double _nextLook;

    public InvisibleEvent? Event { get; private set; }

    public bool IsHidden => Event is not null;

    public bool CanBeHit => Keeps(KeepsCollision);

    public bool CanMove => Keeps(KeepsMovement);

    public bool CanThink => Keeps(InvisibleEvent.KeepsMind);

    private bool Keeps(byte flag) => Event is null || (Event.Flags & flag) != 0;

    public void Begin(InvisibleEvent hide, double now)
    {
        Event = hide;
        _left = hide.Value * 0.001;
        _nextLook = now + Look;
    }

    /// <summary>
    /// The animation of the monster has played on by so many of its own seconds: a hiding for a time runs out by them, not by
    /// the clock.
    /// </summary>
    public void CountDown(double animationSeconds)
    {
        if (Event?.ConditionType == InvisibleEvent.ForTime)
        {
            _left -= animationSeconds;
        }
    }

    public void Clear()
    {
        Event = null;
    }

    /// <summary>
    /// Whether the hiding is over: its time has passed, the life is down to the share, or none of the monsters it waits for lives.
    /// </summary>
    public bool IsOver(double now, int health, int maxHealth, Func<int, bool> isMonsterAlive)
    {
        switch (Event?.ConditionType)
        {
            case null:
                return false;

            case InvisibleEvent.ForTime:
                return _left <= 0;

            case InvisibleEvent.UntilMonstersDie:
                // Looked at once a second.
                if (now < _nextLook)
                {
                    return false;
                }

                _nextLook = now + Look;

                var waits = Event.Waits ?? [];

                for (var index = 0; index < waits.Count; index++)
                {
                    if (isMonsterAlive(waits[index]))
                    {
                        return false;
                    }
                }

                return true;

            default:
                return maxHealth > 0 && (long)health * 1000 / maxHealth <= Event.Value;
        }
    }

    /// <summary>
    /// The notice that the monster has gone; for one who comes in later, a hiding for a time names the time that is left.
    /// </summary>
    public MonsterInvisibleResponse Notice(ActorValue actor)
    {
        var hide = Event!;
        var value = hide.ConditionType == InvisibleEvent.ForTime ? Math.Max(1, (int)Math.Round(_left * 1000)) : hide.Value;

        return new MonsterInvisibleResponse { Actor = actor, Invisible = true, Flags = hide.Flags, Type = hide.ConditionType, Value = value };
    }
}
