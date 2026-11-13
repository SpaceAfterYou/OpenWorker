using System.Numerics;
using Arch.Core;
using Lua;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Skill.Requests;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Ai;

namespace OpenWorker.Lua;

public partial class LuaMaze
{
    /// <summary>
    /// A projectile of the player on its way, stepped with the maze.
    /// </summary>
    private sealed class PlayerFlight(int skill, AttackTrigger trigger, ProjectileFlight flight, SkillPositionValue position, TimeSpan launched)
    {
        public int Skill { get; } = skill;
        public AttackTrigger Trigger { get; } = trigger;
        public ProjectileFlight Flight { get; } = flight;
        public SkillPositionValue Position { get; } = position;
        public TimeSpan Last { get; set; } = launched;

        /// <summary>
        /// Its step failed, and it flies no further.
        /// </summary>
        public bool Failed { get; set; }
    }

    /// <summary>
    /// The client fires a projectile when its animation gets there, and the server follows it to see whom it hits. The shot
    /// leaves from where the server has the player, the way the client aims it; a player who is down fires nothing, and a skill
    /// fires no more shots than its animations let go.
    /// </summary>
    [LuaIgnoreMember]
    public ClaimVerdict FireProjectile(Entity player, SkillProjectileRequest request)
    {
        if (!Started || Find(player) is not { } member || !creatureManager.Vitals.IsAlive(player))
        {
            return ClaimVerdict.Dropped;
        }

        var hero = Ecs.Get<PersonInfoComponent>(player).Hero;

        // Only the skill that is being played fires its projectiles: a skill the server refused, or one cut by another, fires nothing.
        if (member.Playback is not { } run || run.Skill != request.Skill ||
            !actions.TryGetProjectile(request.Skill, hero, request.TriggerIndex, out var trigger))
        {
            return ClaimVerdict.Dropped;
        }

        if (!TakeShot(member, run, trigger.Id))
        {
            return Refuse(member);
        }

        var shooter = Ecs.Get<WorldComponent>(player).Position;
        var flight = ProjectileFlight.Launch(trigger, shooter, RightOf(request.Right, request.Yaw), request.Yaw, request.Direction, request.Target.Identifier);
        var session = ++member.ProjectileSession;

        if (!actions.IsFlownByClient(request.Skill))
        {
            creatureManager.Broadcast(new SkillProjectileBroadcastResponse
            {
                Owner = Ecs.Get<ActorComponent>(player),
                Projectile = default(ProjectileShotValue) with
                {
                    Skill = request.Skill,
                    Attack = request.TriggerIndex,
                    Position = flight.Position,
                    Direction = flight.Direction,
                    Session = session
                }
            });
        }

        var position = default(SkillPositionValue) with { Position = shooter, Angle = request.Yaw };

        // It flies with the steps of the maze from now on.
        member.Flights.Add(new PlayerFlight(request.Skill, trigger, flight, position, Clock.Time));

        return ClaimVerdict.Taken;
    }

    /// <summary>
    /// To the right of the shooter, as the client says, as a direction: a length the client makes up would carry the start of
    /// the shot away from the player. The way the shooter looks gives it when the client's says nothing.
    /// </summary>
    private static Vector3 RightOf(Vector3 right, float yaw)
    {
        var length = right.Length();

        return float.IsFinite(length) && length > 0.001f ? right / length : new Vector3(AiMath.Direction(yaw - 90f), 0f);
    }

    /// <summary>
    /// The projectiles of the player step with the maze; one that has hit what it can or lived its time is gone.
    /// </summary>
    private void StepFlights(Fighter member)
    {
        var flights = member.Flights;

        if (flights.Count == 0)
        {
            return;
        }

        var now = Clock.Time;

        for (var index = 0; index < flights.Count && !Closed && !member.Gone; index++)
        {
            var flight = flights[index];

            try
            {
                var seconds = (float)Math.Max(0d, (now - flight.Last).TotalSeconds);

                flight.Last = now;

                foreach (var explosion in flight.Flight.Advance(seconds, creatureManager.GetFlightTargets()))
                {
                    Run(ExplodeAsync(member, flight.Skill, flight.Trigger, explosion, flight.Position));
                }
            }

            catch (Exception e)
            {
                Logger.ProjectileFlightFailed(e, flight.Skill);

                flight.Failed = true;
            }
        }

        flights.RemoveAll(static e => e.Failed || e.Flight.IsOver);
    }

    private ValueTask ExplodeAsync(Fighter member, int skill, AttackTrigger trigger, Explosion explosion, SkillPositionValue position)
    {
        var yaw = Ecs.Get<WorldComponent>(member.Player).Rotation;
        var area = new AttackArea(trigger, explosion.Position, yaw);
        var targets = trigger.Projectile is { TargetOnly: true }
            ? []
            : creatureManager.FindMonsters((place, body) => area.Touches(place, body.BodyRadius, body.BodyHeight));

        if (explosion.Collider is { } key && creatureManager.FindMonster(key) is { } collider && !targets.Contains(collider))
        {
            targets = [.. targets, collider];
        }

        return ApplyDamageAsync(member, skill, trigger, targets, position, origin: explosion.Position);
    }
}
