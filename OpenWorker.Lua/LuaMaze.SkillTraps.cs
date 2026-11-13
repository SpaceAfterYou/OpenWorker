using System.Numerics;
using Arch.Core;
using Lua;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;

namespace OpenWorker.Lua;

public partial class LuaMaze
{
    /// <summary>
    /// Where a trap stands. It is a class because the player can move it while it hits.
    /// </summary>
    private sealed class TrapSite(Vector3 position, TimeSpan placed)
    {
        public Vector3 Position { get; set; } = position;

        /// <summary>
        /// When the player last moved it, on the clock of the maze.
        /// </summary>
        public TimeSpan MovedAt { get; set; } = placed;

        /// <summary>
        /// How far it may still go at once: the way the mouse saved up while it went slower than it may.
        /// </summary>
        public float Travel { get; set; }
    }

    /// <summary>
    /// A trap outlives the skill that placed it: a new skill does not take it away.
    /// </summary>
    private void PlaceTrap(Fighter member, int skill, AttackTrigger trigger, SkillPositionValue position)
    {
        var info = trigger.Projectile!;
        var attacker = Ecs.Get<WorldComponent>(member.Player);
        var (sin, cos) = MathF.SinCos(attacker.Rotation * MathF.PI / 180f);
        var site = new TrapSite(attacker.Position, Clock.Time);

        if (actions.FollowsMouse(skill))
        {
            // One trap follows the cursor at a time, for as long as it lives: another attack of the skill, or another use of it,
            // places none while it does.
            if (member.MouseTrap is not null)
            {
                return;
            }

            member.MouseTrap = site;

            _schedule.Schedule(Wait(info.LifeTime), () =>
            {
                if (member.MouseTrap == site)
                {
                    member.MouseTrap = null;
                }
            });
        }

        if (!actions.IsFlownByClient(skill))
        {
            creatureManager.Broadcast(new SkillProjectileBroadcastResponse
            {
                Owner = Ecs.Get<ActorComponent>(member.Player),
                Projectile = default(ProjectileShotValue) with
                {
                    Skill = skill,
                    Attack = (short)trigger.Id,
                    Position = attacker.Position,
                    Direction = new Vector3(sin, -cos, 0f),
                    Session = ++member.ProjectileSession
                }
            });
        }

        StrikeTrap(member, skill, trigger, position, site, attacker.Rotation, info.LifeTime);
    }

    /// <summary>
    /// One hit of a trap. A trap that hits again and again waits for its next hit on the clock, as long as its life lasts.
    /// </summary>
    private void StrikeTrap(Fighter member, int skill, AttackTrigger trigger, SkillPositionValue position, TrapSite site, float yaw, float left)
    {
        if (Closed || member.Gone)
        {
            return;
        }

        var info = trigger.Projectile!;

        // The place is read again at every hit: the player may have moved the trap since the last one.
        var area = new AttackArea(trigger, site.Position, yaw);
        var targets = creatureManager.FindMonsters((place, body) => area.Touches(place, body.BodyRadius, body.BodyHeight));

        Run(ApplyDamageAsync(member, skill, trigger, targets, position, origin: site.Position));

        if (!info.IsContinuous || info.ContinuousTime <= 0f || left <= info.ContinuousTime)
        {
            return;
        }

        _schedule.Schedule(Wait(info.ContinuousTime), () => StrikeTrap(member, skill, trigger, position, site, yaw, left - info.ContinuousTime));
    }

    /// <summary>
    /// How far from the player the client puts a trap it drags with the mouse (the skill's table gives it no reach): the pointer
    /// meets the ground at most 2500 from the camera, or lies 2500 from the player where it meets nothing, and the camera behind
    /// the player and the moves the server has not heard of yet add to that.
    /// </summary>
    private const float MouseReach = 3500f;

    /// <summary>
    /// How fast the trap may follow the mouse, in units a second: the client moves it at most 700 a second; the rest lets a trap
    /// the server held back on a burst of late messages catch up.
    /// </summary>
    private const float TrapSpeed = 875f;

    /// <summary>
    /// The most way a trap may save up while it goes slower than it may: one second of it.
    /// </summary>
    private const float TrapTravel = TrapSpeed;

    /// <summary>
    /// The player drags the trap of a skill with the mouse. The trap hits where it is; the others see only the hits. A place out
    /// of the reach of the mouse from where the server has the player is refused, and the trap stays.
    /// </summary>
    [LuaIgnoreMember]
    public ClaimVerdict MoveTrap(Entity player, Vector3 place)
    {
        return MoveTrap(player, place, out _);
    }

    /// <summary>
    /// The player drags the trap of a skill with the mouse, as <see cref="MoveTrap(Entity, Vector3)"/>. The trap goes toward the
    /// place no faster than the client moves it, so a place far from where it stands is reached over the next moments.
    /// </summary>
    /// <param name="taken">Where the trap stands now.</param>
    [LuaIgnoreMember]
    public ClaimVerdict MoveTrap(Entity player, Vector3 place, out Vector3 taken)
    {
        taken = place;

        if (!Started || Find(player) is not { } member || !creatureManager.Vitals.IsAlive(player))
        {
            return ClaimVerdict.Dropped;
        }

        var from = Ecs.Get<WorldComponent>(player).Position;

        // The pointer meets the ground or what stands on it: high or low, it is within the reach of the mouse.
        if (!(Vector3.DistanceSquared(from, place) <= MouseReach * MouseReach))
        {
            return Refuse(member);
        }

        if (member.MouseTrap is { } trap)
        {
            var travel = Math.Min(TrapTravel, trap.Travel + ((float)(Clock.Time - trap.MovedAt).TotalSeconds * TrapSpeed));
            var way = place - trap.Position;
            var length = way.Length();

            if (length > travel)
            {
                place = trap.Position + (way * (travel / length));
                length = travel;
            }

            trap.Travel = travel - length;
            trap.MovedAt = Clock.Time;
            trap.Position = place;
            taken = place;
        }

        return ClaimVerdict.Taken;
    }
}
