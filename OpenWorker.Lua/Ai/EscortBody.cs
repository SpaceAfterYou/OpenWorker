using System.Numerics;
using Arch.Core;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Ai;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Lua.Managers;

namespace OpenWorker.Lua.Ai;

/// <summary>
/// A blow of the escorted that lands on monsters, dealt by the maze once all minds have thought.
/// </summary>
internal readonly record struct EscortStrike(Entity Escorted, int Skill, AttackTrigger Trigger, Entity[] Targets);

/// <summary>
/// The escorted townsman as its mind sees it: it patrols the way of escort points the maze gives it, point after point, and
/// fights the monsters of a side at war with its own on the way. Its blows hurt monsters, never players.
/// </summary>
internal sealed class EscortBody : MonsterBody, IAiHost
{
    private Vector3? _way;

    public EscortBody(World ecs, LuaCreatureManager manager, SkillActionProvider actions, Entity entity, MonsterBehavior behavior, AiProfile profile, Vector3 home)
        : base(ecs, manager, actions, entity, behavior, profile, home)
    {
    }

    /// <summary>
    /// The blows that landed since the maze last dealt them.
    /// </summary>
    public List<EscortStrike> Strikes { get; } = [];

    Vector3? IAiHost.WayPoint => _way;

    /// <summary>
    /// The escorted belongs where its point is: it comes back there after a fight.
    /// </summary>
    public override Vector3 HomePosition => _way ?? base.HomePosition;

    /// <summary>
    /// The escorted heads for the next point of its way.
    /// </summary>
    public void Follow(Vector3 point)
    {
        _way = point;
    }

    public override AiTarget? GetTarget(int id)
    {
        return FindEnemyEntity(id) is { } monster ? Describe(monster) : null;
    }

    public override AiTarget? FindEnemy(float distance)
    {
        var from = Position;
        AiTarget? best = null;
        var nearest = distance;

        foreach (var creature in Manager.Living)
        {
            if (!IsEnemy(creature))
            {
                continue;
            }

            var target = Describe(creature);
            var between = Vector3.Distance(from, target.Position) - target.Radius;

            if (between <= nearest && Manager.Navigation.CanSee(from, target.Position))
            {
                nearest = between;
                best = target;
            }
        }

        return best;
    }

    public override int CountEnemies(float distance)
    {
        var from = Position;

        return Manager.Living.Count(e => IsEnemy(e) && Vector3.Distance(from, Ecs.Get<WorldComponent>(e).Position) <= distance);
    }

    protected override bool Strike(int skill, AttackTrigger trigger, AttackArea area, int? collider, bool colliderOnly)
    {
        var targets = colliderOnly ? [] : Touching(area);

        if (collider is { } key && FindEnemyEntity(key) is { } hit && !targets.Contains(hit))
        {
            targets = [.. targets, hit];
        }

        if (targets.Length == 0 || !trigger.AffectsTargets || trigger.ForAllies || trigger.Buff > 0 && trigger.DamageRate <= 0f)
        {
            return false;
        }

        Strikes.Add(new EscortStrike(Entity, skill, trigger, targets));

        return true;
    }

    protected override void CollectFlightTargets(List<FlightTarget> targets)
    {
        var living = Manager.Living;

        for (var index = 0; index < living.Count; index++)
        {
            var creature = living[index];

            if (IsEnemy(creature))
            {
                var combat = Ecs.Get<MonsterCombatComponent>(creature);

                targets.Add(new FlightTarget(Ecs.Get<ActorComponent>(creature), Ecs.Get<WorldComponent>(creature).Position, combat.BodyRadius, combat.BodyHeight));
            }
        }
    }

    protected override IReadOnlyList<Entity> TrapTargets(AttackTrigger trigger, AttackArea area) => Touching(area);

    protected override void StrikeByTrap(int skill, AttackTrigger trigger, Vector3 place, AttackArea area, IReadOnlyList<Entity> targets, bool round)
    {
        if (targets.Count > 0 && trigger.AffectsTargets && !trigger.ForAllies && !(trigger.Buff > 0 && trigger.DamageRate <= 0f))
        {
            Strikes.Add(new EscortStrike(Entity, skill, trigger, [.. targets]));
        }
    }

    /// <summary>
    /// A monster the escorted fights: shown to the players, alive, not itself defended, and of a side at war with the escorted.
    /// </summary>
    private bool IsEnemy(Entity creature)
    {
        if (creature == Entity || !Ecs.IsAlive(creature) || Ecs.Get<ActorComponent>(creature).Type != ActorType.Monster || !Manager.IsSeen(creature) ||
            Manager.Helpers.Defended.Contains(creature))
        {
            return false;
        }

        var monster = Ecs.Get<CreatureComponent>(creature);

        return monster.Health > 0
            && (Manager.Services is not { } services || services.Behaviors.Get(monster.Identifier) is not { } row || services.Behaviors.AreEnemies(Behavior.Faction, row.Faction));
    }

    private Entity? FindEnemyEntity(int id)
    {
        foreach (var creature in Manager.Living)
        {
            if (Ecs.IsAlive(creature) && (int)Ecs.Get<ActorComponent>(creature) == id)
            {
                return IsEnemy(creature) ? creature : null;
            }
        }

        return null;
    }

    private Entity[] Touching(AttackArea area)
    {
        return Manager.Living
            .Where(IsEnemy)
            .Where(e => area.Touches(Ecs.Get<WorldComponent>(e).Position, Ecs.Get<MonsterCombatComponent>(e).BodyRadius, Ecs.Get<MonsterCombatComponent>(e).BodyHeight))
            .ToArray();
    }

    private AiTarget Describe(Entity monster)
    {
        var place = Ecs.Get<WorldComponent>(monster);
        var combat = Ecs.Get<MonsterCombatComponent>(monster);

        return new AiTarget(Ecs.Get<ActorComponent>(monster), place.Position, combat.BodyRadius, place.Rotation, Ecs.Get<CreatureComponent>(monster).Health, (int)combat.Stats.MaxHealth, false);
    }
}
