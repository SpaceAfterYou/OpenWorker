using System.Numerics;
using Arch.Core;
using OpenWorker.Batch.Actions;
using OpenWorker.Gameplay.Ai;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;

namespace OpenWorker.Lua.Ai;

/// <summary>
/// The traps of a monster: areas its attacks leave on the ground. A trap waits for its delay, then hits whoever comes into it,
/// each of them again every interval when it hits continuously, and when its life is over it bursts over its own space. Some
/// burst at once when somebody comes near, some only at the end. A trap outlives the skill that made it, and a flinch of its
/// maker does not take it away.
/// </summary>
internal partial class MonsterBody
{
    /// <summary>
    /// How a trap behaves after its delay, as its event names it: the kinds 0, 1, 2 and 6 hit whoever comes in, 4 and 5 burst
    /// when somebody comes near, the others only burst at the end.
    /// </summary>
    private const short BurstsWhenNear = 4;
    private const short BurstsAfterNear = 5;

    private readonly List<PlacedTrap> _traps = [];
    private readonly List<TrapRun> _trapRuns = [];

    /// <summary>
    /// The key of the scattered traps, as the start of the skill told the clients; every scattered trap moves it on.
    /// </summary>
    private int _trapKey;

    /// <summary>
    /// Where the monster last marked its target standing.
    /// </summary>
    private Vector3? _trapMark;

    private sealed class PlacedTrap(int skill, AttackTrigger trigger, TrapInfo info, Vector3 position, float yaw)
    {
        public int Skill { get; } = skill;
        public AttackTrigger Trigger { get; } = trigger;
        public TrapInfo Info { get; } = info;
        public Vector3 Position { get; } = position;

        /// <summary>
        /// The way the trap lies from its maker, which turns the space it bursts over.
        /// </summary>
        public float Yaw { get; } = yaw;

        public float Delay { get; set; } = info.Delay;
        public float Life { get; set; } = trigger.Projectile!.LifeTime;

        /// <summary>
        /// How long each target in the trap waits for its next hit.
        /// </summary>
        public Dictionary<Entity, float> Waits { get; } = [];

        /// <summary>
        /// How long the helpers in the trap wait for its next hit, which they take together.
        /// </summary>
        public float RoundWait { get; set; }

        public bool HasStarted { get; set; }

        public bool IsOver { get; set; }
    }

    /// <summary>
    /// The traps of a pattern still to come, one each interval.
    /// </summary>
    private sealed class TrapRun(int skill, AttackTrigger trigger, TrapInfo info, Vector3 position, float yaw)
    {
        public int Skill { get; } = skill;
        public AttackTrigger Trigger { get; } = trigger;
        public TrapInfo Info { get; } = info;
        public Vector3 Position { get; } = position;
        public float Yaw { get; } = yaw;
        public int Left { get; set; } = info.Count - 1;
        public float Time { get; set; }
    }

    /// <summary>
    /// The monster marks where its target stands, on the ground; with no target, where it stands itself.
    /// </summary>
    private void MarkTrapPlace()
    {
        _trapMark = Machine.TargetId != AiMachine.NoTarget && GetTarget(Machine.TargetId) is { } target ? OnFloor(target.Position) : Position;
    }

    /// <summary>
    /// The event of an attack places its traps: one where the attack lands or where the target was marked, several at random
    /// round the monster, or a row from the place of the attack. A pattern with an interval places the first now and the rest
    /// one by one.
    /// </summary>
    private void SetTraps(int skill, AttackTrigger trigger)
    {
        if (trigger.Projectile?.Trap is not { } info)
        {
            return;
        }

        var position = Position;
        var yaw = Yaw;
        var spread = info.Interval > 0f && info.Count > 1;

        switch (info.Pattern)
        {
            case TrapInfo.Scattered:
                if (spread)
                {
                    _trapRuns.Add(new TrapRun(skill, trigger, info, position, yaw));
                }

                for (var i = 0; i < (spread ? 1 : info.Count); i++)
                {
                    PlaceTrap(skill, trigger, info, TrapLayout.Scatter(info, position, yaw, ref _trapKey));
                }

                break;

            case TrapInfo.InRow:
                var start = position + TrapLayout.Turn(trigger.Center, yaw);

                if (spread)
                {
                    _trapRuns.Add(new TrapRun(skill, trigger, info, start, yaw));
                }

                for (var i = 0; i < (spread ? 1 : info.Count); i++)
                {
                    PlaceTrap(skill, trigger, info, TrapLayout.InRow(info, start, yaw, i));
                }

                break;

            default:
                PlaceTrap(skill, trigger, info, info.GoesToMark ? _trapMark ?? MarkedNow() : position + TrapLayout.Turn(trigger.Center, yaw));

                break;
        }
    }

    private Vector3 MarkedNow()
    {
        MarkTrapPlace();

        return _trapMark!.Value;
    }

    private void PlaceTrap(int skill, AttackTrigger trigger, TrapInfo info, Vector3 place)
    {
        var position = Position;
        var yaw = AiMath.Distance2D(position, place) < 1f ? Yaw : AiMath.YawTo(position, place);

        _traps.Add(new PlacedTrap(skill, trigger, info, place, yaw));

        // The clients make the traps of a monster themselves; only one that follows its target is told.
        if (trigger.Projectile!.IsGuided)
        {
            Broadcast(new SkillProjectileBroadcastResponse
            {
                Owner = Actor,
                Projectile = default(ProjectileShotValue) with
                {
                    Skill = skill,
                    Attack = (short)trigger.Id,
                    Position = place,
                    Direction = new Vector3(AiMath.Direction(Yaw), 0f),
                    Yaw = Yaw,
                    Session = ++_shotSession
                }
            });
        }
    }

    private void TickTraps(float elapsed)
    {
        for (var i = 0; i < _trapRuns.Count; i++)
        {
            var run = _trapRuns[i];

            run.Time += elapsed;

            if (run.Time < run.Info.Interval)
            {
                continue;
            }

            var place = run.Info.Pattern == TrapInfo.Scattered
                ? TrapLayout.Scatter(run.Info, run.Position, run.Yaw, ref _trapKey)
                : TrapLayout.InRow(run.Info, run.Position, run.Yaw, run.Info.Count - run.Left);

            PlaceTrap(run.Skill, run.Trigger, run.Info, place);

            run.Left--;
            run.Time -= run.Info.Interval;
        }

        _trapRuns.RemoveAll(static e => e.Left <= 0);

        // In place: a trap that is over leaves the list after the round.
        for (var i = 0; i < _traps.Count; i++)
        {
            var trap = _traps[i];

            if (trap.IsOver)
            {
                continue;
            }

            // A trap with no life bursts at once.
            if (trap.Delay > 0f && trap.Life > 0f)
            {
                trap.Delay -= elapsed;
            }

            trap.Life -= elapsed;

            if (trap.Life <= 0f)
            {
                Burst(trap);

                continue;
            }

            if (trap.Delay > 0f)
            {
                continue;
            }

            switch (trap.Trigger.Projectile!.ExplodeDecision)
            {
                case 0 or 1 or 2 or 6:
                    Influence(trap, elapsed);
                    break;

                case BurstsWhenNear or BurstsAfterNear when BurstVictims(trap).Count > 0:
                    Burst(trap);
                    break;
            }
        }

        _traps.RemoveAll(static e => e.IsOver);
    }

    /// <summary>
    /// The trap hits whoever is in it: one that comes in at once, then again every interval while it stays, if the trap hits
    /// continuously.
    /// </summary>
    private void Influence(PlacedTrap trap, float elapsed)
    {
        if (InfluenceArea(trap) is not { } area)
        {
            return;
        }

        var interval = trap.Trigger.Projectile!.IsContinuous ? trap.Trigger.Projectile.ContinuousTime : 0f;
        List<Entity>? due = null;

        foreach (var target in TrapTargets(trap.Trigger, area))
        {
            if (!trap.Waits.TryGetValue(target, out var wait))
            {
                trap.Waits[target] = interval;
                (due ??= []).Add(target);
            }
            else if (interval > 0f)
            {
                wait -= elapsed;

                if (wait <= 0f)
                {
                    wait = interval;
                    (due ??= []).Add(target);
                }

                trap.Waits[target] = wait;
            }
        }

        var round = !trap.HasStarted;

        if (trap.HasStarted && interval > 0f)
        {
            trap.RoundWait -= elapsed;
            round = trap.RoundWait <= 0f;
        }

        if (round)
        {
            trap.RoundWait = interval;
        }

        trap.HasStarted = true;

        StrikeByTrap(trap.Skill, trap.Trigger, trap.Position, area, due ?? [], round);
    }

    /// <summary>
    /// The trap is over: it bursts over its space and hits everyone in it, if it has a space to burst over.
    /// </summary>
    private void Burst(PlacedTrap trap)
    {
        trap.IsOver = true;

        if (trap.Info.Burst is not { Radius: > 0f } burst)
        {
            return;
        }

        StrikeByTrap(trap.Skill, trap.Trigger, trap.Position, BurstArea(trap, burst), BurstVictims(trap), true);
    }

    private IReadOnlyList<Entity> BurstVictims(PlacedTrap trap)
    {
        return trap.Info.Burst is { Radius: > 0f } burst ? TrapTargets(trap.Trigger, BurstArea(trap, burst)) : [];
    }

    /// <summary>
    /// What a trap hits while it lies: a circle of its radius round it, no higher or lower than its radius, within the height of
    /// its attack. Null when the two heights do not meet.
    /// </summary>
    private static AttackArea? InfluenceArea(PlacedTrap trap)
    {
        var trigger = trap.Trigger;
        var bottom = Math.Max(-trigger.Radius, trigger.Center.Z);
        var top = Math.Min(trigger.Radius, trigger.Center.Z + trigger.Height);
        if (top < bottom)
        {
            return null;
        }

        var circle = trigger with { RangeType = 0, Angle = 360f, Center = new Vector3(0f, 0f, bottom), StartPosition = 0f, Height = top - bottom };

        return new AttackArea(circle, trap.Position, trap.Yaw);
    }

    private static AttackArea BurstArea(PlacedTrap trap, TrapBurst burst)
    {
        var space = trap.Trigger with
        {
            RangeType = 0,
            Angle = burst.Angle,
            Center = burst.Center,
            Radius = burst.Radius,
            Height = burst.Height,
            StartPosition = burst.StartPosition
        };

        return new AttackArea(space, trap.Position, trap.Yaw);
    }

    /// <summary>
    /// Whom the traps of the monster reach in a space: the players, by the way they stand.
    /// </summary>
    protected virtual IReadOnlyList<Entity> TrapTargets(AttackTrigger trigger, AttackArea area)
    {
        return Reachable(trigger, _manager.PlayersTouching(area));
    }

    /// <summary>
    /// A trap of the monster hits the targets that are due, each told on its own as the clients expect; on a round of the trap
    /// the helpers in it are hit too.
    /// </summary>
    protected virtual void StrikeByTrap(int skill, AttackTrigger trigger, Vector3 place, AttackArea area, IReadOnlyList<Entity> targets, bool round)
    {
        foreach (var target in targets)
        {
            _manager.HitPlayers(Entity, skill, trigger, [target], place);
        }

        if (round && _manager.Helpers.HasTargets)
        {
            _manager.Helpers.HitBy(this, skill, trigger, area, null, false);
        }
    }
}
