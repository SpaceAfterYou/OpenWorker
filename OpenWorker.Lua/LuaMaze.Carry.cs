using System.Numerics;
using Arch.Core;
using Lua;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Movement.Responses;
using OpenWorker.Lua.Ai;
using OpenWorker.Lua.Managers;

namespace OpenWorker.Lua;

/// <summary>
/// A skill of a player that carries away the monsters before it. For the time the event of the skill names, whatever stands
/// in front of the player and is not too heavy is told to go where the player goes; a monster that is too heavy stops the
/// player. The rules are in <see cref="CarryRules"/>.
/// </summary>
public partial class LuaMaze
{
    /// <summary>
    /// A monster the player carries, and how it stands before the player.
    /// </summary>
    private readonly record struct Carried(Entity Monster, MonsterBody Body, Vector3 Direction, float Distance);

    /// <summary>
    /// The time in which a skill looks for monsters to carry, and the ones it has.
    /// </summary>
    private sealed class CarryRun(AttachEvent attach, TimeSpan until)
    {
        public AttachEvent Attach { get; } = attach;
        public TimeSpan Until { get; } = until;
        public List<Carried> Taken { get; } = [];

        /// <summary>
        /// A monster too heavy has stopped the player, which is told once.
        /// </summary>
        public bool Stopped { get; set; }
    }

    /// <summary>
    /// The moments at which the skill that begins now starts to carry. A skill that is replaced by another before its moment has
    /// nothing to carry.
    /// </summary>
    private void PlanCarries(Fighter member, int skill, Hero hero, int generation, TimeSpan began)
    {
        foreach (var carry in actions.GetCarries(skill, hero))
        {
            _schedule.ScheduleAt(began + Wait(carry.Time), () =>
            {
                if (!Closed && !member.Gone && member.SkillGeneration == generation)
                {
                    member.Carry = new CarryRun(carry.Event, Clock.Time + Wait(carry.Event.Duration));
                }
            });
        }
    }

    private void StepCarry(Fighter member)
    {
        if (member.Carry is not { } run)
        {
            return;
        }

        if (member.Gone || !Ecs.IsAlive(member.Player) || Clock.Time >= run.Until)
        {
            EndCarry(member);

            return;
        }

        var carrier = Ecs.Get<WorldComponent>(member.Player);

        FollowCarried(run, carrier);
        var area = new AttackArea(CarryRules.AreaOf(run.Attach, PlayerVitals.BodyHeight), carrier.Position, carrier.Rotation);

        foreach (var monster in creatureManager.FindMonsters((place, body) => area.Touches(place, body.BodyRadius, body.BodyHeight)))
        {
            if (creatureManager.BodyOf(monster) is not { IsCarried: false, IsLying: false, IsDead: false } body || run.Taken.Any(e => e.Monster == monster))
            {
                continue;
            }

            var combat = Ecs.Get<MonsterCombatComponent>(monster);

            if (CarryRules.IsTooHeavy(combat.WeightRank, combat.StandType, run.Attach.WeightRank))
            {
                // It does not give: the player stops against it.
                if (!run.Stopped)
                {
                    run.Stopped = true;

                    creatureManager.Broadcast(new MovementIgnoreMotionDeltaResponse
                    {
                        Actor = Ecs.Get<ActorComponent>(member.Player),
                        Pos = carrier.Position,
                        Yaw = carrier.Rotation,
                        Pitch = 0f,
                        Forced = 1
                    });
                }

                return;
            }

            var between = body.Position - carrier.Position;
            var distance = between.Length();

            body.BeginCarried();
            run.Taken.Add(new Carried(monster, body, distance > float.Epsilon ? between / distance : new Vector3(0f, -1f, 0f), distance));

            creatureManager.Broadcast(new MovementAttacedBtResponse
            {
                Owner = Ecs.Get<ActorComponent>(member.Player),
                Actor = Ecs.Get<ActorComponent>(monster),
                Direction = run.Taken[^1].Direction,
                Distance = distance
            });
        }
    }

    /// <summary>
    /// Whoever is carried goes where the player takes it, unless the ground stops it, and is let go there.
    /// </summary>
    private void FollowCarried(CarryRun run, WorldComponent carrier)
    {
        for (var index = run.Taken.Count - 1; index >= 0; index--)
        {
            var carried = run.Taken[index];

            if (!Ecs.IsAlive(carried.Monster) || carried.Body.IsDead || !carried.Body.IsCarried)
            {
                run.Taken.RemoveAt(index);

                continue;
            }

            var radius = Ecs.Get<MonsterCombatComponent>(carried.Monster).BodyRadius;
            var wanted = CarryRules.Follow(carrier.Position, carrier.Rotation, PlayerVitals.BodyRadius, radius, carried.Direction, carried.Distance, run.Attach.Offset);
            var place = creatureManager.Navigation.Clamp(carried.Body.Position, wanted, radius, out var reached);

            if (reached)
            {
                carried.Body.CarriedTo(place);
            }
            else
            {
                carried.Body.EndCarried();
                run.Taken.RemoveAt(index);
            }
        }
    }

    private void EndCarry(Fighter member)
    {
        if (member.Carry is { } run)
        {
            foreach (var carried in run.Taken.Where(e => Ecs.IsAlive(e.Monster) && !e.Body.IsDead))
            {
                carried.Body.EndCarried();
            }
        }

        member.Carry = null;
    }
}
