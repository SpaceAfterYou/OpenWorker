using OpenWorker.Batch.Actions;
using OpenWorker.Hotspot.Modules.Monster.Responses;

namespace OpenWorker.Lua.Ai;

internal partial class MonsterBody
{
    private readonly MonsterHiding _hiding = new();
    private readonly List<(double Time, InvisibleEvent Event)> _hidings = [];

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
        return _hiding.IsHidden ? _hiding.Notice(Actor, _manager.Now) : null;
    }

    private void ResolveHidings(double now)
    {
        if (_hiding.IsOver(now, Health, MaxHealth, _manager.IsMonsterAlive))
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
