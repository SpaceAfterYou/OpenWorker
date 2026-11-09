using System.Numerics;
using Arch.Core;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Ai;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Mapping;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Helpers.Responses;
using OpenWorker.Hotspot.Modules.Helpers.Types;
using OpenWorker.Hotspot.Modules.Monster.Responses;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Lua.Ai;
using OpenWorker.Lua.Managers;

namespace OpenWorker.Lua.Helpers;

/// <summary>
/// The helpers the players of a maze have called. A helper is a monster of its own table row that follows the person who
/// called it: the monsters may fight it, it fights the monsters, and everything it does is shown to every player of the
/// maze. It never dies of a blow; it goes when its owner sends it away, falls, or leaves the maze.
/// </summary>
public sealed class HelperSquad
{
    private static ComponentType[] Archetype { get; } = [
        ComponentRegistry.Add<ActorComponent>(),
        ComponentRegistry.Add<CreatureComponent>(),
        ComponentRegistry.Add<NonPlayableCreatureComponent>(),
        ComponentRegistry.Add<WorldComponent>(),
        ComponentRegistry.Add<MonsterCombatComponent>(),
    ];

    /// <summary>
    /// How far from its owner, along the way the owner looks, a helper appears.
    /// </summary>
    private const float SpawnDistance = 100f;

    /// <summary>
    /// How far above or below the place the ground may be for the helper to appear there.
    /// </summary>
    private const float GroundReach = 200f;

    private const float AggroPerDamage = 0.0004f;

    private readonly World _ecs;
    private readonly LuaCreatureManager _manager;
    private readonly CombatTables _tables;
    private readonly List<SummonedHelper> _helpers = [];
    private readonly Dictionary<Entity, double> _lastSummon = [];
    private readonly List<HelperStrike> _strikes = [];

    internal HelperSquad(World ecs, LuaCreatureManager manager, CombatTables tables)
    {
        _ecs = ecs;
        _manager = manager;
        _tables = tables;

        // A person who falls takes its helpers with it.
        manager.Vitals.HealthChanged += OnHealthChanged;
    }

    /// <summary>
    /// How many helpers stand in the maze, of all its players.
    /// </summary>
    public int Count => _helpers.Count;

    public IReadOnlyList<SummonedHelper> All => _helpers;

    /// <summary>
    /// Where the blows of the helpers go: the maze deals them as it deals those of a player.
    /// </summary>
    internal Action<HelperStrike>? Strikes { get; set; }

    /// <summary>
    /// A helper has used a skill that is not meant for an enemy on its owner: the helper and the skill.
    /// </summary>
    public event Action<SummonedHelper, int>? OwnerSkillUsed;

    /// <summary>
    /// A helper has been sent away, whatever the reason.
    /// </summary>
    public event Action<SummonedHelper>? Released;

    public IEnumerable<SummonedHelper> Of(Entity owner) => _helpers.Where(e => e.Owner == owner);

    public SummonedHelper? Find(Entity owner, int helper) => _helpers.Find(e => e.Owner == owner && e.Helper == helper);

    /// <summary>
    /// How long ago the person last called a helper into this maze.
    /// </summary>
    public TimeSpan SinceLastSummon(Entity owner)
    {
        return _lastSummon.TryGetValue(owner, out var at) ? TimeSpan.FromSeconds(Math.Max(0d, _manager.Now - at)) : TimeSpan.MaxValue;
    }

    // ---- Calling and sending away -------------------------------------------------------------

    /// <summary>
    /// The helper appears ahead of its owner (on the owner's own place when there is no ground there), with the stats it
    /// has at the owner's level, its owner as its parent, and is shown to every player of the maze; then everybody hears
    /// whose helper it is. Nothing when the tables do not know the helper.
    /// </summary>
    internal SummonedHelper? Summon(Entity owner, CombatStats stats, HelperValue record)
    {
        if (!_ecs.IsAlive(owner) || !_ecs.Has<WorldComponent>(owner) || _manager.Services is not { } services
            || services.Behaviors.Get(record.Helper) is not { } behavior)
        {
            return null;
        }

        var place = _ecs.Get<WorldComponent>(owner);
        var ownerActor = _ecs.Get<ActorComponent>(owner);
        var actor = new ActorComponent(_manager.NextActor(), ActorType.Monster);
        var entity = _ecs.Create(Archetype);

        _ecs.Set(entity, actor);
        _ecs.Set(entity, new CreatureComponent { Identifier = record.Helper, Level = stats.Level, Health = (int)Math.Max(1f, stats.MaxHealth) });
        _ecs.Set(entity, new NonPlayableCreatureComponent());
        _ecs.Set(entity, place with { Position = SpawnPlace(place), Jump = 0 });
        _ecs.Set(entity, _tables.GetMonster(record.Helper) with
        {
            Stats = stats,
            SuperArmor = 0f,
            MaxSuperArmor = 0f,
            HoldsSector = false
        });

        var helper = new SummonedHelper(entity, actor, owner, ownerActor, record, behavior.Faction);

        helper.Body = new HelperBody(_ecs, _manager, services.Actions, helper, behavior, services.Scripts.Get(behavior.AiScript), this);

        _helpers.Add(helper);
        _lastSummon[owner] = _manager.Now;

        _manager.Broadcast(new WorldInInfoMonsterResponse { Monsters = [Describe(helper)] });

        helper.Body.Reveal();

        _manager.Broadcast(Announcement(helper, true));

        return helper;
    }

    private Vector3 SpawnPlace(in WorldComponent owner)
    {
        var ahead = AiMath.Direction(owner.Rotation) * SpawnDistance;
        var place = owner.Position + new Vector3(ahead, 0f);

        return _manager.Navigation.TryGetFloor(place, out var height) && MathF.Abs(height - place.Z) <= GroundReach
            ? place with { Z = height }
            : owner.Position;
    }

    private STMonsterInfo Describe(SummonedHelper helper)
    {
        return MonsterSnapshotMapper.ToStMonsterInfo(_ecs, helper.Entity, helper.Body?.IsBattlePose ?? false) with { uxParentActorID = helper.OwnerActor };
    }

    private static HelperSummonResponse Announcement(SummonedHelper helper, bool summoned)
    {
        return new HelperSummonResponse
        {
            Person = helper.OwnerActor,
            Actor = helper.Actor,
            Summon = summoned ? (byte)1 : (byte)0,
            Helper = helper.Record with { Summoned = summoned }
        };
    }

    /// <summary>
    /// The helper goes: it falls with nothing to give (no drop, no experience), then everybody hears it was sent away.
    /// </summary>
    public void Release(SummonedHelper helper)
    {
        if (!_helpers.Remove(helper))
        {
            return;
        }

        _manager.Broadcast(new MonsterDieResponse { Actor = helper.Actor });
        _manager.Broadcast(Announcement(helper, false));

        if (_ecs.IsAlive(helper.Entity))
        {
            _ecs.Destroy(helper.Entity);
        }

        Released?.Invoke(helper);
    }

    /// <summary>
    /// Every helper of the person goes.
    /// </summary>
    public void ReleaseAll(Entity owner)
    {
        foreach (var helper in Of(owner).ToArray())
        {
            Release(helper);
        }
    }

    /// <summary>
    /// The person leaves the maze: its helpers go, and its client forgets the helpers of the others.
    /// </summary>
    internal void OnLeaving(Entity owner)
    {
        ReleaseAll(owner);

        _lastSummon.Remove(owner);

        if (_helpers.Count == 0 || !_ecs.IsAlive(owner) || !_ecs.Has<ServerSessionComponent>(owner))
        {
            return;
        }

        var session = _ecs.Get<ServerSessionComponent>(owner);

        foreach (var helper in _helpers)
        {
            session.Send(Announcement(helper, false));
        }
    }

    /// <summary>
    /// The person calls its helpers back: each of them stands where the person stands at once.
    /// </summary>
    public void Recall(Entity owner)
    {
        foreach (var helper in Of(owner).ToArray())
        {
            helper.Body?.Machine.Recall();
        }
    }

    /// <summary>
    /// A player who comes into the maze sees the helpers that are in it and hears whose they are.
    /// </summary>
    public void ShowTo(ServerSessionComponent session)
    {
        if (_helpers.Count == 0)
        {
            return;
        }

        session.Send(new WorldOtherInfosMonsterResponse { Monsters = _helpers.Select(Describe).ToArray() });

        foreach (var helper in _helpers)
        {
            session.Send(Announcement(helper, true));
        }
    }

    private void OnHealthChanged(Entity player)
    {
        if (_helpers.Count > 0 && !_manager.Vitals.IsAlive(player))
        {
            ReleaseAll(player);
        }
    }

    internal void OnOwnerSkill(SummonedHelper helper, int skill) => OwnerSkillUsed?.Invoke(helper, skill);

    // ---- Thinking -----------------------------------------------------------------------------

    /// <summary>
    /// The helpers think after the monsters, and their blows land when all of them have thought.
    /// </summary>
    internal void Step(double now, float seconds)
    {
        if (_helpers.Count == 0)
        {
            return;
        }

        foreach (var helper in _helpers.ToArray())
        {
            if (helper.Body is not { } body || !_ecs.IsAlive(helper.Entity))
            {
                continue;
            }

            try
            {
                body.Update(now, seconds);
            }

            catch (Exception e)
            {
                _manager.Logger.MonsterFailedToThink(e, helper.Helper);
            }
        }

        if (_strikes.Count == 0)
        {
            return;
        }

        var strikes = _strikes.ToArray();

        _strikes.Clear();

        foreach (var strike in strikes)
        {
            if (_helpers.Contains(strike.Helper))
            {
                Strikes?.Invoke(strike);
            }
        }
    }

    internal void Queue(HelperStrike strike) => _strikes.Add(strike);

    internal void Clear()
    {
        foreach (var helper in _helpers)
        {
            if (_ecs.IsAlive(helper.Entity))
            {
                _ecs.Destroy(helper.Entity);
            }
        }

        _helpers.Clear();
        _strikes.Clear();
        _lastSummon.Clear();
    }

    // ---- The helpers as the monsters see them ------------------------------------------------

    /// <summary>
    /// The person a helper follows, as its mind sees it.
    /// </summary>
    internal AiTarget? Describe(Entity owner)
    {
        if (!_ecs.IsAlive(owner) || !_ecs.Has<AbilityComponent>(owner) || !_ecs.Has<WorldComponent>(owner))
        {
            return null;
        }

        var place = _ecs.Get<WorldComponent>(owner);

        return new AiTarget(_ecs.Get<ActorComponent>(owner), place.Position, PlayerVitals.BodyRadius, place.Rotation, _manager.Vitals.Health(owner), _manager.Vitals.MaxHealth(owner), false);
    }

    private AiTarget Target(SummonedHelper helper)
    {
        var place = _ecs.Get<WorldComponent>(helper.Entity);
        var combat = _ecs.Get<MonsterCombatComponent>(helper.Entity);

        return new AiTarget(helper.Actor, place.Position, combat.BodyRadius, place.Rotation, _ecs.Get<CreatureComponent>(helper.Entity).Health, (int)combat.Stats.MaxHealth, helper.Body?.IsMoving == true);
    }

    /// <summary>
    /// Whether a monster may fight a helper: their sides are at war.
    /// </summary>
    private bool IsFoe(MonsterBody monster, SummonedHelper helper)
    {
        return _ecs.IsAlive(helper.Entity) && (_manager.Services is not { } services || services.Behaviors.AreEnemies(monster.Behavior.Faction, helper.Faction));
    }

    /// <summary>
    /// A helper a monster is angry with, by its actor.
    /// </summary>
    internal AiTarget? FindTarget(int id)
    {
        foreach (var helper in _helpers)
        {
            if ((int)helper.Actor == id && _ecs.IsAlive(helper.Entity))
            {
                return Target(helper);
            }
        }

        return null;
    }

    /// <summary>
    /// The nearer of a player and the helpers the monster may go for. A monster of a high rank does not look for helpers.
    /// </summary>
    internal AiTarget? Nearest(MonsterBody monster, float distance, AiTarget? player)
    {
        if (monster.Behavior.Rank >= MonsterBehavior.IgnoresFollowersFromRank)
        {
            return player;
        }

        var from = monster.Position;
        var best = player;
        var nearest = player is { } found ? Vector3.Distance(from, found.Position) - found.Radius : distance;

        foreach (var helper in _helpers)
        {
            if (!IsFoe(monster, helper))
            {
                continue;
            }

            var target = Target(helper);
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
        var from = monster.Position;
        var count = 0;

        foreach (var helper in _helpers)
        {
            if (IsFoe(monster, helper) && Vector3.Distance(from, _ecs.Get<WorldComponent>(helper.Entity).Position) <= distance)
            {
                count++;
            }
        }

        return count;
    }

    internal IEnumerable<FlightTarget> FlightTargets(MonsterBody monster)
    {
        foreach (var helper in _helpers)
        {
            if (IsFoe(monster, helper))
            {
                var combat = _ecs.Get<MonsterCombatComponent>(helper.Entity);

                yield return new FlightTarget(helper.Actor, _ecs.Get<WorldComponent>(helper.Entity).Position, combat.BodyRadius, combat.BodyHeight);
            }
        }
    }

    /// <summary>
    /// A blow of a monster lands on the helpers in its area, or on the one its projectile ran into. A helper that is getting
    /// up takes nothing; one whose life would reach zero keeps one point and starts to get up. Everybody sees the blow.
    /// </summary>
    internal void HitBy(MonsterBody attacker, int skillId, AttackTrigger trigger, AttackArea area, int? collider, bool colliderOnly)
    {
        if (!trigger.AffectsTargets || !_ecs.IsAlive(attacker.Entity))
        {
            return;
        }

        var stats = _ecs.Get<MonsterCombatComponent>(attacker.Entity).Stats;
        var skill = _tables.GetSkill(skillId);
        var attackerActor = _ecs.Get<ActorComponent>(attacker.Entity);
        var hits = new List<SkillDmgValue>();
        var beaten = new List<SummonedHelper>();

        foreach (var helper in _helpers)
        {
            if (!IsFoe(attacker, helper) || helper.Body is not { } body)
            {
                continue;
            }

            var combat = _ecs.Get<MonsterCombatComponent>(helper.Entity);
            var place = _ecs.Get<WorldComponent>(helper.Entity).Position;

            if (!(collider == (int)helper.Actor || !colliderOnly && area.Touches(place, combat.BodyRadius, combat.BodyHeight)))
            {
                continue;
            }

            var creature = _ecs.Get<CreatureComponent>(helper.Entity);
            var shielded = helper.IsRecovering || combat.Defense == DefenseType.Invincible;
            var result = shielded
                ? new DamageResult(0, HitResults.None)
                : DamageCalculator.Calculate(stats, combat.Stats, skill, combat.Defense, trigger.DamageRate, _manager.Random);
            var left = creature.Health - result.Damage;

            if (left <= 0)
            {
                left = 1;
                beaten.Add(helper);
            }

            _ecs.Set(helper.Entity, creature with { Health = left });

            hits.Add(default(SkillDmgValue) with
            {
                UxActorId = helper.Actor,
                ReactionType = shielded ? (byte)0 : (byte)trigger.ReactionType,
                DamageFlags = (byte)result.Flags,
                Damage = result.Damage,
                Health = left,
                DefenseType = (byte)(shielded ? DefenseType.Invincible : combat.Defense)
            });

            if (!shielded)
            {
                body.Machine.OnDamaged(attackerActor, result.Damage * AggroPerDamage);
                body.Machine.OnEscapeBlow(collider is null ? AiMachine.SkillBlow : AiMachine.ProjectileBlow);
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

        // Beaten down, a helper gets up instead of dying.
        foreach (var helper in beaten)
        {
            helper.Body?.Machine.Recover();
        }
    }

    // ---- The monsters as the helpers see them -------------------------------------------------

    /// <summary>
    /// Whether a creature is a monster the helper fights: shown to the players, alive, and of a side at war with the helper's.
    /// </summary>
    private bool IsEnemy(HelperBody helper, Entity creature)
    {
        if (!_ecs.IsAlive(creature) || _ecs.Get<ActorComponent>(creature).Type != ActorType.Monster || !_manager.IsSeen(creature))
        {
            return false;
        }

        var monster = _ecs.Get<CreatureComponent>(creature);

        return monster.Health > 0
            && (_manager.Services is not { } services || services.Behaviors.Get(monster.Identifier) is not { } row || services.Behaviors.AreEnemies(helper.Helper.Faction, row.Faction));
    }

    private AiTarget Target(Entity monster)
    {
        var place = _ecs.Get<WorldComponent>(monster);
        var combat = _ecs.Get<MonsterCombatComponent>(monster);

        return new AiTarget(_ecs.Get<ActorComponent>(monster), place.Position, combat.BodyRadius, place.Rotation, _ecs.Get<CreatureComponent>(monster).Health, (int)combat.Stats.MaxHealth, false);
    }

    internal Entity? FindEnemyEntity(HelperBody helper, int id)
    {
        foreach (var creature in _manager.Living)
        {
            if ((int)_ecs.Get<ActorComponent>(creature) == id)
            {
                return IsEnemy(helper, creature) ? creature : null;
            }
        }

        return null;
    }

    internal AiTarget? FindEnemy(HelperBody helper, int id)
    {
        return FindEnemyEntity(helper, id) is { } monster ? Target(monster) : null;
    }

    internal AiTarget? NearestEnemy(HelperBody helper, float distance)
    {
        var from = helper.Position;
        AiTarget? best = null;
        var nearest = distance;

        foreach (var creature in _manager.Living)
        {
            if (!IsEnemy(helper, creature))
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

    internal int CountEnemies(HelperBody helper, float distance)
    {
        var from = helper.Position;

        return _manager.Living.Count(e => IsEnemy(helper, e) && Vector3.Distance(from, _ecs.Get<WorldComponent>(e).Position) <= distance);
    }

    internal Entity[] EnemiesTouching(HelperBody helper, AttackArea area)
    {
        return _manager.Living
            .Where(e => IsEnemy(helper, e))
            .Where(e => area.Touches(_ecs.Get<WorldComponent>(e).Position, _ecs.Get<MonsterCombatComponent>(e).BodyRadius, _ecs.Get<MonsterCombatComponent>(e).BodyHeight))
            .ToArray();
    }

    internal FlightTarget[] EnemyFlightTargets(HelperBody helper)
    {
        return _manager.Living
            .Where(e => IsEnemy(helper, e))
            .Select(e => new FlightTarget(_ecs.Get<ActorComponent>(e), _ecs.Get<WorldComponent>(e).Position, _ecs.Get<MonsterCombatComponent>(e).BodyRadius, _ecs.Get<MonsterCombatComponent>(e).BodyHeight))
            .ToArray();
    }
}
