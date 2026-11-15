using OpenWorker.Batch.Actions;
using OpenWorker.Hotspot.Modules.Monster.Responses;

namespace OpenWorker.Lua.Ai;

internal partial class MonsterBody
{
    /// <summary>
    /// How fast the animation of a monster plays while a blow it dealt holds it.
    /// </summary>
    private const float HeldSpeed = 0.005f;

    private readonly MonsterHiding _hiding = new();
    private readonly List<(double Time, InvisibleEvent Event)> _hidings = [];

    // The stretches of the skill that plays whose animation runs at a speed of its own; outside them it runs at 1.
    private readonly List<(double Start, double End, float Speed)> _speeds = [];
    private Func<int, bool>? _isMonsterAlive;

    /// <summary>
    /// The monster has hidden and cannot be hit or aimed at while it is gone.
    /// </summary>
    public bool IsOutOfReach => !_hiding.CanBeHit;

    /// <summary>
    /// The kinds of hiding the body follows. The kind that waits for the monsters it summoned is not: nothing summons in a maze yet.
    /// </summary>
    internal static bool IsFollowed(InvisibleEvent hide)
    {
        return hide.ConditionType is InvisibleEvent.ForTime or InvisibleEvent.UntilHurt or InvisibleEvent.UntilMonstersDie;
    }

    /// <summary>
    /// What a player who comes in while the monster is gone is told after the list of monsters.
    /// </summary>
    public MonsterInvisibleResponse? HidingNotice()
    {
        return _hiding.IsHidden ? _hiding.Notice(Actor) : null;
    }

    /// <summary>
    /// How many seconds of its own the animation of the monster has played between two moments of the clock.
    /// </summary>
    private double AnimationSeconds(double from, double to)
    {
        var seconds = to - from;

        foreach (var (start, end, speed) in _speeds)
        {
            var overlap = Math.Min(to, end) - Math.Max(from, start);

            if (overlap > 0)
            {
                seconds += overlap * (speed - 1f);
            }
        }

        return seconds;
    }

    /// <summary>
    /// A blow holds the animation from the moment for so many seconds: the stretches still to come come that much later.
    /// </summary>
    private void HoldSpeeds(double now, float seconds)
    {
        var count = _speeds.Count;

        for (var i = 0; i < count; i++)
        {
            var (start, end, speed) = _speeds[i];

            if (end <= now)
            {
                continue;
            }

            if (start >= now)
            {
                _speeds[i] = (start + seconds, end + seconds, speed);

                continue;
            }

            _speeds[i] = (start, now, speed);
            _speeds.Add((now + seconds, end + seconds, speed));
        }

        _speeds.Add((now, now + seconds, HeldSpeed));
    }

    private void ResolveHidings(double now, float elapsed)
    {
        _hiding.CountDown(AnimationSeconds(now - elapsed, now));

        if (_hiding.IsOver(now, Health, MaxHealth, _isMonsterAlive ??= _manager.IsMonsterAlive))
        {
            _hiding.Clear();

            Broadcast(new MonsterInvisibleResponse { Actor = Actor, Invisible = false });
        }

        while (_hidings.Count > 0 && _hidings[0].Time <= now)
        {
            var hide = _hidings[0].Event;

            _hidings.RemoveAt(0);
            _hiding.Begin(hide, now);

            Broadcast(new MonsterInvisibleResponse { Actor = Actor, Invisible = true, Flags = hide.Flags, Type = hide.ConditionType, Value = hide.Value });
        }
    }
}
