using System.Numerics;
using Arch.Core;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Ai;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Helpers.Responses;
using OpenWorker.Lua.Ai;
using OpenWorker.Lua.Managers;

namespace OpenWorker.Lua.Helpers;

/// <summary>
/// A helper as its mind sees it: a monster whose enemies are the monsters of the maze, whose owner is the person who
/// called it, and whose blows the maze deals as it deals those of a player.
/// </summary>
internal sealed class HelperBody : MonsterBody
{
    private readonly HelperSquad _squad;

    public HelperBody(World ecs, LuaCreatureManager manager, SkillActionProvider actions, SummonedHelper helper, MonsterBehavior behavior, AiProfile profile, HelperSquad squad)
        : base(ecs, manager, actions, helper.Entity, behavior, profile, ecs.Get<WorldComponent>(helper.Entity).Position)
    {
        _squad = squad;

        Helper = helper;
    }

    public SummonedHelper Helper { get; }

    public override AiTarget? Owner => _squad.Describe(Helper.Owner);

    /// <summary>
    /// A helper keeps to its owner: where it wanders it wanders around the owner.
    /// </summary>
    public override Vector3 HomePosition => Owner is { } owner ? owner.Position : base.HomePosition;

    public override AiTarget? GetTarget(int id) => _squad.FindEnemy(this, id);

    public override AiTarget? FindEnemy(float distance) => _squad.NearestEnemy(this, distance);

    public override int CountEnemies(float distance) => _squad.CountEnemies(this, distance);

    /// <summary>
    /// Put next to its owner, a helper is shown there by the message the client keeps for helpers.
    /// </summary>
    public override void WarpTo(Vector3 position)
    {
        StopMoving(false);

        Ecs.Set(Entity, Ecs.Get<WorldComponent>(Entity) with { Position = position });

        Broadcast(new HelperSyncPosResponse { Actor = Helper.Actor, X = position.X, Y = position.Y, Z = position.Z });
    }

    public override bool UseSkill(AiSkill skill, AiTarget target)
    {
        var used = base.UseSkill(skill, target);

        if (used && skill.TargetsOwner)
        {
            _squad.OnOwnerSkill(Helper, skill.Id);
        }

        return used;
    }

    protected override void Strike(int skill, AttackTrigger trigger, AttackArea area, int? collider, bool colliderOnly)
    {
        var targets = colliderOnly ? [] : _squad.EnemiesTouching(this, area);

        if (collider is { } key && _squad.FindEnemyEntity(this, key) is { } hit && !targets.Contains(hit))
        {
            targets = [.. targets, hit];
        }

        // A blow for the side of the helper (its owner, the party, itself) gives its buff and hurts nobody; one that only
        // carries a buff gives it to the monsters it reaches.
        if (trigger.ForAllies)
        {
            _squad.GiveToAllies(Helper, trigger);

            return;
        }

        if (trigger.Buff > 0 && trigger.DamageRate <= 0f)
        {
            Manager.Buffs?.GiveFromHit(Entity, trigger, targets);

            return;
        }

        if (targets.Length > 0 && trigger.AffectsTargets)
        {
            _squad.Queue(new HelperStrike(Helper, skill, trigger, targets));
        }
    }

    protected override FlightTarget[] FlightTargets() => _squad.EnemyFlightTargets(this);
}
