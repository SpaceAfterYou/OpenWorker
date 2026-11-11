using Arch.Core;
using Lua;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Ai;
using OpenWorker.Lua.Ai;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Helpers.Types;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Lua.Helpers;

namespace OpenWorker.Lua;

/// <summary>
/// The helpers the players call into the maze: whether the maze takes them, calling, sending away, calling back, and the
/// blows of a helper, which the maze deals as it deals those of a player and credits to the helper's owner.
/// </summary>
public partial class LuaMaze
{
    /// <summary>
    /// The kind of maze that never takes helpers.
    /// </summary>
    private const byte NoHelpersMazeType = 12;

    /// <summary>
    /// The helpers called into the maze, of all its players.
    /// </summary>
    [LuaIgnoreMember]
    public HelperSquad Helpers => creatureManager.Helpers;

    /// <summary>
    /// The maze does not take helpers: its table says so, or it is of the kind that never does.
    /// </summary>
    [LuaIgnoreMember]
    public bool RefusesHelpers => info.RefuseHelpers != 0 || info.Type == NoHelpersMazeType;

    /// <summary>
    /// A player of the maze calls a helper with these stats. Nothing when the maze is over, the player is not in it, or the
    /// tables do not know the helper.
    /// </summary>
    [LuaIgnoreMember]
    public SummonedHelper? SummonHelper(Entity owner, CombatStats stats, HelperValue record)
    {
        if (Closed || Find(owner) is null)
        {
            return null;
        }

        creatureManager.Helpers.Strikes ??= strike => Run(StrikeAsync(strike));

        return creatureManager.Helpers.Summon(owner, stats, record);
    }

    /// <summary>
    /// A player sends one of its helpers away. False when that helper is not called.
    /// </summary>
    [LuaIgnoreMember]
    public bool ReleaseHelper(Entity owner, int helper)
    {
        if (creatureManager.Helpers.Find(owner, helper) is not { } called)
        {
            return false;
        }

        creatureManager.Helpers.Release(called);

        return true;
    }

    /// <summary>
    /// A player calls its helpers back to where it stands.
    /// </summary>
    [LuaIgnoreMember]
    public void RecallHelpers(Entity owner)
    {
        creatureManager.Helpers.Recall(owner);
    }

    /// <summary>
    /// The monsters a helper hit take the blow as from a player, and those that die are the owner's kills: what they drop,
    /// the experience, the quests and the missions.
    /// </summary>
    private async ValueTask StrikeAsync(HelperStrike strike)
    {
        var helper = strike.Helper;

        if (Closed || !Ecs.IsAlive(helper.Entity))
        {
            return;
        }

        var targets = strike.Targets.Where(e => Ecs.IsAlive(e) && Ecs.Get<CreatureComponent>(e).Health > 0).ToArray();

        if (targets.Length == 0)
        {
            return;
        }

        var owner = helper.Owner;
        var attacker = Ecs.Get<WorldComponent>(helper.Entity);
        var stats = _buffs.StatsOf(helper.Entity, Ecs.Get<MonsterCombatComponent>(helper.Entity).Stats);
        var skill = combat.GetSkill(strike.Skill);
        var hits = targets.Select(target => ApplyDamage(helper.Entity, target, stats, skill, strike.Trigger.DamageRate, strike.Trigger, strike.Skill, HitKinds.Of(strike.Trigger))).ToArray();

        creatureManager.TellHitSlow(hits, strike.Trigger);

        creatureManager.Broadcast(new SkillActionBroadcastResponse
        {
            SkillDmgValueList = hits,
            SkillAction = default(SkillActionValue) with
            {
                SkillActorInfo = default(SkillActorInfoValue) with
                {
                    UxActorId = helper.Actor,
                    SkillPosInfo = default(SkillActionPosInfoValue) with
                    {
                        Position = attacker.Position,
                        Angle = attacker.Rotation,
                        MotionClass = -1
                    }
                },
                Skill = strike.Skill,
                TriggerIndex = (short)strike.Trigger.Id,
                ContinuousHit = 1
            }
        });

        // The buff of the blow reaches the monsters it hit and left standing.
        _buffs.GiveFromHit(helper.Entity, strike.Trigger, targets);

        var dead = targets
            .Select((target, i) => (Target: target, Hit: hits[i]))
            .Where(e => Ecs.Get<CreatureComponent>(e.Target).Health == 0)
            .Select(e => (Removed: RemoveDead(e.Target), e.Hit))
            .ToArray();

        // What a helper kills, its owner has killed.
        if (Ecs.IsAlive(owner))
        {
            foreach (var ((identifier, _, fell), _) in dead)
            {
                MonsterKilled?.Invoke(owner, identifier, fell);
            }
        }

        var kind = actions.GetPassiveType(strike.Skill);

        foreach (var ((identifier, place, _), hit) in dead)
        {
            if (Closed)
            {
                return;
            }

            if (Ecs.IsAlive(owner) && Find(owner) is not null)
            {
                await ReportKillAsync(owner, identifier, MazeReporter.GetKillConditions(kind, (hit.DamageFlags & (byte)Gameplay.Combat.HitResults.Critical) != 0));
            }

            await State.OnDie(identifier, place.SpawnBox, this);
        }

        foreach (var sector in dead.Select(e => e.Removed.Place.Sector).Distinct())
        {
            if (Closed)
            {
                return;
            }

            await CheckSectorAsync(sector);
        }
    }
}
