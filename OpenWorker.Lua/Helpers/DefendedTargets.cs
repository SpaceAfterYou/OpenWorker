using System.Numerics;
using Arch.Core;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Ai;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Hotspot.Modules.Monster.Responses;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Lua.Ai;
using OpenWorker.Lua.Managers;

namespace OpenWorker.Lua.Helpers;

/// <summary>
/// The monsters a mode of a maze asks the players to defend. To the monsters such a monster is an enemy like a helper: they
/// see it, go for it and hit it, and a blow that takes the last of its life fells it. The players cannot hit it, and its own
/// mind does nothing; the mode watches its life.
/// </summary>
public sealed class DefendedTargets
{
    private readonly World _ecs;
    private readonly LuaCreatureManager _manager;
    private readonly CombatTables _tables;
    private readonly List<Entity> _targets = [];

    internal DefendedTargets(World ecs, LuaCreatureManager manager, CombatTables tables)
    {
        _ecs = ecs;
        _manager = manager;
        _tables = tables;
    }

    /// <summary>
    /// Whether the monsters have anything to defend.
    /// </summary>
    public bool Any => _targets.Count > 0;

    public bool Contains(Entity creature) => _targets.Contains(creature);

    /// <summary>
    /// The monster is now defended: the monsters go for it, the players cannot hit it, and it starts with its life whole.
    /// </summary>
    public void Add(Entity creature)
    {
        if (!_ecs.IsAlive(creature) || _targets.Contains(creature))
        {
            return;
        }

        var combat = _ecs.Get<MonsterCombatComponent>(creature);

        _ecs.Set(creature, _ecs.Get<CreatureComponent>(creature) with { Health = (int)combat.Stats.MaxHealth });
        _targets.Add(creature);
    }

    public void Remove(Entity creature)
    {
        _targets.Remove(creature);
        _led.Remove(creature);
    }

    public void Clear()
    {
        _targets.Clear();
        _led.Clear();
    }

    private readonly HashSet<Entity> _led = [];

    /// <summary>
    /// The maze walks the defended monster itself: its own mind and body do nothing until it is no longer defended.
    /// </summary>
    public void Lead(Entity creature)
    {
        if (_targets.Contains(creature))
        {
            _led.Add(creature);
        }
    }

    public bool IsLed(Entity creature) => _led.Contains(creature);

    /// <summary>
    /// The life of a defended monster, zero when it has fallen or is gone.
    /// </summary>
    public int Health(Entity creature)
    {
        return _ecs.IsAlive(creature) && _ecs.Has<CreatureComponent>(creature) ? _ecs.Get<CreatureComponent>(creature).Health : 0;
    }

    private AiTarget Target(Entity creature)
    {
        var place = _ecs.Get<WorldComponent>(creature);
        var combat = _ecs.Get<MonsterCombatComponent>(creature);

        return new AiTarget(_ecs.Get<ActorComponent>(creature), place.Position, combat.BodyRadius, place.Rotation, _ecs.Get<CreatureComponent>(creature).Health, (int)combat.Stats.MaxHealth, false);
    }

    /// <summary>
    /// A defended monster still standing is an enemy of every monster but itself.
    /// </summary>
    private bool IsFoe(MonsterBody monster, Entity creature)
    {
        return monster.Entity != creature && _ecs.IsAlive(creature) && _ecs.Get<CreatureComponent>(creature).Health > 0;
    }

    /// <summary>
    /// The defended monster of an actor, for a monster that is angry with it.
    /// </summary>
    internal AiTarget? Find(int id)
    {
        foreach (var creature in _targets)
        {
            if (_ecs.IsAlive(creature) && (int)_ecs.Get<ActorComponent>(creature) == id && _ecs.Get<CreatureComponent>(creature).Health > 0)
            {
                return Target(creature);
            }
        }

        return null;
    }

    /// <summary>
    /// The nearest of the target found so far and the defended monsters the monster sees.
    /// </summary>
    internal AiTarget? Nearest(MonsterBody monster, float distance, AiTarget? best)
    {
        var from = monster.Position;
        var nearest = best is { } found ? Vector3.Distance(from, found.Position) - found.Radius : distance;

        foreach (var creature in _targets)
        {
            if (!IsFoe(monster, creature))
            {
                continue;
            }

            var target = Target(creature);
            var between = Vector3.Distance(from, target.Position) - target.Radius;

            if (between <= nearest && _manager.Navigation.CanSee(from, target.Position))
            {
                nearest = between;
                best = target;
            }
        }

        return best;
    }

    internal int CountFoes(MonsterBody monster, float distance)
    {
        return _targets.Count(e => IsFoe(monster, e) && Vector3.Distance(monster.Position, _ecs.Get<WorldComponent>(e).Position) <= distance);
    }

    internal IEnumerable<FlightTarget> FlightTargets(MonsterBody monster)
    {
        foreach (var creature in _targets)
        {
            if (IsFoe(monster, creature))
            {
                var combat = _ecs.Get<MonsterCombatComponent>(creature);

                yield return new FlightTarget(_ecs.Get<ActorComponent>(creature), _ecs.Get<WorldComponent>(creature).Position, combat.BodyRadius, combat.BodyHeight);
            }
        }
    }

    /// <summary>
    /// A blow of a monster lands on the defended monsters in its area, or on the one its projectile ran into. Everybody sees the
    /// blow; a monster whose life it takes falls, and the players see it die.
    /// </summary>
    internal void HitBy(MonsterBody attacker, int skillId, AttackTrigger trigger, AttackArea area, int? collider, bool colliderOnly)
    {
        if (!trigger.AffectsTargets || !_ecs.IsAlive(attacker.Entity) || trigger.ForAllies || (trigger.Buff > 0 && trigger.DamageRate <= 0f))
        {
            return;
        }

        var buffs = _manager.Buffs;
        var combatOfAttacker = _ecs.Get<MonsterCombatComponent>(attacker.Entity);
        var stats = buffs?.StatsOf(attacker.Entity, combatOfAttacker.Stats) ?? combatOfAttacker.Stats;
        var skill = _tables.GetSkill(skillId);
        var attackerActor = _ecs.Get<ActorComponent>(attacker.Entity);
        var hits = new List<SkillDmgValue>();
        var fallen = new List<Entity>();

        foreach (var creature in _targets)
        {
            if (!IsFoe(attacker, creature))
            {
                continue;
            }

            var combat = _ecs.Get<MonsterCombatComponent>(creature);
            var place = _ecs.Get<WorldComponent>(creature).Position;
            var actor = _ecs.Get<ActorComponent>(creature);

            if (!(collider == (int)actor || !colliderOnly && area.Touches(place, combat.BodyRadius, combat.BodyHeight)))
            {
                continue;
            }

            var state = _ecs.Get<CreatureComponent>(creature);
            var defense = buffs?.DefenseOf(creature, combat.Defense) ?? combat.Defense;
            var result = defense == DefenseType.Invincible
                ? new DamageResult(0, HitResults.None)
                : DamageCalculator.Calculate(stats, buffs?.StatsOf(creature, combat.Stats) ?? combat.Stats, skill, defense, trigger.DamageRate, _manager.Random);
            var left = Math.Max(0, state.Health - result.Damage);

            _ecs.Set(creature, state with { Health = left });

            hits.Add(default(SkillDmgValue) with
            {
                UxActorId = actor,
                ReactionType = 0,
                DamageFlags = (byte)result.Flags,
                Damage = result.Damage,
                Health = left,
                DefenseType = (byte)defense
            });

            if (left == 0)
            {
                fallen.Add(creature);
            }
        }

        if (hits.Count == 0)
        {
            return;
        }

        var position = _ecs.Get<WorldComponent>(attacker.Entity);

        _manager.Broadcast(new SkillActionBroadcastResponse
        {
            SkillDmgValueList = hits.ToArray(),
            SkillAction = default(SkillActionValue) with
            {
                SkillActorInfo = default(SkillActorInfoValue) with
                {
                    UxActorId = attackerActor,
                    SkillPosInfo = default(SkillActionPosInfoValue) with
                    {
                        Position = position.Position,
                        Angle = position.Rotation,
                        MotionClass = -1
                    }
                },
                Skill = skillId,
                TriggerIndex = (short)trigger.Id,
                ContinuousHit = 1
            }
        });

        foreach (var creature in fallen)
        {
            _manager.Broadcast(new MonsterDieResponse { Actor = _ecs.Get<ActorComponent>(creature) });
        }
    }
}
