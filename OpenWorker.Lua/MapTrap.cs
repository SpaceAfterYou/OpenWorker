using System.Numerics;
using Arch.Core;
using OpenWorker.Batch.Actions;

namespace OpenWorker.Lua;

/// <summary>
/// A trap a maze script lays on the map. It lives for the time of its attack event on the game time of the maze. One that
/// lingers hits what comes into it at once and, while it stays, again at every interval of the event; one that explodes on
/// touch hits what touches its core once and is gone; the rest hit what stands on their core when their life is over.
/// </summary>
public sealed class MapTrap
{
    /// <summary>
    /// The kinds of explosion of an event that blow up when something touches the core of the trap.
    /// </summary>
    private const short ExplodesOnTouch = 4;
    private const short ExplodesAfterDelay = 5;

    /// <summary>
    /// Whom an attack is for beyond the kinds the trigger names: anyone but the attacker, and anyone.
    /// </summary>
    public const int AnyoneButSelf = 4;
    public const int Anyone = 5;

    private readonly Dictionary<Entity, float> _waits = [];

    /// <param name="yaw">The way a trap laid in a box faces; a trap laid at a place has none and hits around itself.</param>
    public MapTrap(int skill, AttackTrigger trigger, Vector3 position, float? yaw, uint session)
    {
        Skill = skill;
        Trigger = trigger;
        Position = position;
        Yaw = yaw;
        Session = session;
        Life = trigger.Projectile?.LifeTime ?? 0f;
    }

    public int Skill { get; }

    public AttackTrigger Trigger { get; }

    public Vector3 Position { get; }

    public float? Yaw { get; }

    /// <summary>
    /// How the client knows this trap among the ones of its owner.
    /// </summary>
    public uint Session { get; }

    /// <summary>
    /// The seconds the trap still lives.
    /// </summary>
    public float Life { get; set; }

    public bool Active { get; private set; } = true;

    private short Explosion => Trigger.Projectile?.ExplodeDecision ?? 0;

    /// <summary>
    /// The kinds that linger; every other kind explodes, on touch or at its end.
    /// </summary>
    private bool Lingers => Explosion is 0 or 1 or 2 or 6;

    private bool ExplodesOnContact => Explosion is ExplodesOnTouch or ExplodesAfterDelay;

    /// <summary>
    /// The seconds between two hits of the same target that stays in the trap; zero hits each target once.
    /// </summary>
    private float Interval => Trigger.Projectile is { IsContinuous: true, ContinuousTime: > 0f and var time } ? time : 0f;

    /// <summary>
    /// Whether the trap hurts or holds the players.
    /// </summary>
    public bool ReachesPlayers => !Trigger.ForAllies;

    /// <summary>
    /// Whether the trap reaches the monsters, which stand on the side of its owner.
    /// </summary>
    public bool ReachesMonsters => Trigger.TargetType is AttackTrigger.SelfAndFriends or AttackTrigger.Friends or AnyoneButSelf or Anyone;

    /// <summary>
    /// What a lingering trap covers: a ball of the radius of its event around it, or, laid in a box, the area of its event turned
    /// the way the box faces.
    /// </summary>
    public AttackArea Area => Yaw is { } yaw && Trigger.RangeType != SphereRange
        ? Trigger.RangeType == CoreRange ? Ball(Trigger.Projectile?.Radius ?? 0f) : new AttackArea(Trigger, Position, yaw)
        : Ball(Trigger.Radius);

    /// <summary>
    /// The core of the trap, which an exploding trap waits to be touched at.
    /// </summary>
    public AttackArea Core => Ball(Trigger.Projectile?.Radius ?? 0f);

    // How a trap laid in a box reads the range type of its event: the core of the trap, or a ball.
    private const short CoreRange = 0;
    private const short SphereRange = 2;

    /// <summary>
    /// The script sets the time the trap still lives.
    /// </summary>
    public void LiveFor(float seconds)
    {
        Life = float.IsFinite(seconds) ? seconds : 0f;
    }

    /// <summary>
    /// The trap lives <paramref name="elapsed"/> seconds more and says whom it hits now. <paramref name="inside"/> gives the
    /// targets in an area.
    /// </summary>
    public IReadOnlyList<Entity> Step(float elapsed, Func<AttackArea, IReadOnlyList<Entity>> inside)
    {
        if (!Active)
        {
            return [];
        }

        if (Life > 0f)
        {
            Life -= elapsed;
        }

        if (Life <= 0f)
        {
            Active = false;

            // At its end a trap that does not explode on touch hits what stands on its core.
            return ExplodesOnContact ? [] : inside(Core);
        }

        if (!Lingers)
        {
            if (!ExplodesOnContact)
            {
                return [];
            }

            var touching = inside(Core);

            if (touching.Count > 0)
            {
                Active = false;
            }

            return touching;
        }

        var hit = new List<Entity>();

        foreach (var target in inside(Area))
        {
            if (!_waits.TryGetValue(target, out var wait))
            {
                _waits.Add(target, Interval);
                hit.Add(target);

                continue;
            }

            if (Interval <= 0f)
            {
                continue;
            }

            wait -= elapsed;

            if (wait <= 0f)
            {
                wait = Interval;
                hit.Add(target);
            }

            _waits[target] = wait;
        }

        return hit;
    }

    /// <summary>
    /// A ball around the trap: a full circle, as high above as below it.
    /// </summary>
    private AttackArea Ball(float radius)
    {
        return new AttackArea(Trigger with { RangeType = 0, Center = new Vector3(0f, 0f, -radius), Radius = radius, Height = 2f * radius, StartPosition = 0f, Angle = 360f }, Position, 0f);
    }
}
