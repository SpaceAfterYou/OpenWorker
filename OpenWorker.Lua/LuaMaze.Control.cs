using System.Numerics;
using Arch.Core;
using Lua;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.Gameplay.Ai;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Monster.Responses;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;

namespace OpenWorker.Lua;

/// <summary>
/// A monster a player controls through a skill: it comes in at the moment the skill's animation says, everybody in the maze is
/// told (17/51) and hides the player, and the player's attack keys make it play the skills of its row (17/52). It is on the
/// player's side: its blows are the player's, shown as the monster's, and what they kill counts for the player. The moment of the
/// skill that lets it go, a new monster of the player, leaving the maze, or an attack key pressed after a fall ends the control.
/// </summary>
public partial class LuaMaze
{
    private const short ControlNoMotion = -1;

    /// <summary>
    /// The monster a player controls.
    /// </summary>
    private sealed class Controlled(ActorValue actor, MonsterBehavior behavior, Vector3 place, bool follows, int generation)
    {
        public ActorValue Actor { get; } = actor;
        public MonsterBehavior Behavior { get; } = behavior;

        /// <summary>
        /// Where it came in; one that follows the player is where the player is.
        /// </summary>
        public Vector3 Place { get; } = place;

        public bool Follows { get; } = follows;

        /// <summary>
        /// The play of the player's skill that brought it in: only that play lets it go.
        /// </summary>
        public int Generation { get; } = generation;

        /// <summary>
        /// Until when the skill it plays lasts: it takes no other before.
        /// </summary>
        public TimeSpan BusyUntil { get; set; }
    }

    private readonly Dictionary<Fighter, Controlled> _controlled = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// The moments at which the skill that begins now takes control of a monster or lets it go. A skill cut before its monster
    /// comes brings none; the moment of the release comes all the same, as the clients keep the monster until they are told.
    /// </summary>
    private void PlanControls(Fighter member, int skill, Hero hero, int generation, TimeSpan began)
    {
        foreach (var control in actions.GetControls(skill, hero))
        {
            _schedule.ScheduleAt(began + Wait(control.Time), control.Event.Monster != 0
                ? () => TakeControl(member, control, generation)
                : () => EndControl(member, generation));
        }
    }

    private void TakeControl(Fighter member, SkillControl control, int generation)
    {
        if (Closed || member.Gone || member.SkillGeneration != generation || !creatureManager.Vitals.IsAlive(member.Player))
        {
            return;
        }

        // A monster the table does not have cannot be made by the clients either.
        if (creatureManager.Services?.Behaviors.Get(control.Event.Monster) is not { } behavior)
        {
            return;
        }

        var owner = Ecs.Get<WorldComponent>(member.Player);
        var (sin, cos) = MathF.SinCos(owner.Rotation * MathF.PI / 180f);
        var offset = control.Event.Offset;
        var place = owner.Position + new Vector3(offset.X * cos - offset.Y * sin, offset.X * sin + offset.Y * cos, 0f);

        TakeControl(member, behavior, control.Event.Monster, place, control.Event.Alpha, control.Event.Blend, control.Animation, control.Event.Follows, generation);
    }

    /// <summary>
    /// The player takes a monster of that row in place of the one it had, and everybody in the maze is told.
    /// </summary>
    private void TakeControl(Fighter member, MonsterBehavior behavior, int table, Vector3 place, float alpha, float blend, int animation, bool follows,
        int generation)
    {
        EndControl(member);

        var actor = new ActorValue(creatureManager.NextActor(), ActorType.Monster);

        _controlled[member] = new Controlled(actor, behavior, place, follows, generation);

        creatureManager.Broadcast(new MonsterControlResponse
        {
            Owner = Ecs.Get<ActorComponent>(member.Player),
            Monster = actor,
            Position = place,
            Yaw = Ecs.Get<WorldComponent>(member.Player).Rotation,
            Table = table,
            Alpha = alpha,
            Blend = blend,
            Animation = animation,
            Follows = follows
        });
    }

    /// <summary>
    /// The skill that brought the monster in lets it go.
    /// </summary>
    private void EndControl(Fighter member, int generation)
    {
        if (_controlled.TryGetValue(member, out var controlled) && controlled.Generation == generation)
        {
            EndControl(member);
        }
    }

    /// <summary>
    /// The player lets its monster go: everybody in the maze is told, and the player shows again.
    /// </summary>
    private void EndControl(Fighter member)
    {
        if (!_controlled.Remove(member))
        {
            return;
        }

        if (Closed || !Ecs.IsAlive(member.Player) || !Ecs.Has<ActorComponent>(member.Player))
        {
            return;
        }

        creatureManager.Broadcast(new MonsterControlResponse
        {
            Owner = Ecs.Get<ActorComponent>(member.Player),
            Yaw = Ecs.Get<WorldComponent>(member.Player).Rotation,
            Animation = MonsterControlResponse.NoAnimation
        });
    }

    /// <summary>
    /// The player's attack key makes its monster play a skill of its row: the first key the first skill, the second the second.
    /// A monster that is not the player's, one let go a moment ago, or one that still plays its last skill does nothing; a skill
    /// the row cannot have is a claim the client cannot make.
    /// </summary>
    [LuaIgnoreMember]
    public ClaimVerdict ControlAttack(Entity player, int monster, byte attack)
    {
        if (!Started || Find(player) is not { } member || !_controlled.TryGetValue(member, out var controlled) || (int)controlled.Actor != monster)
        {
            return ClaimVerdict.Dropped;
        }

        if (!creatureManager.Vitals.IsAlive(player))
        {
            EndControl(member);

            return ClaimVerdict.Dropped;
        }

        var skills = controlled.Behavior.Skills;

        if (attack >= skills.Count || skills[attack] == 0)
        {
            return ClaimVerdict.Refused;
        }

        if (Clock.Time < controlled.BusyUntil)
        {
            return ClaimVerdict.Dropped;
        }

        var skill = skills[attack];
        var hits = actions.GetHits(skill, controlled.Behavior.ActionFile);
        var length = creatureManager.Services?.Behaviors.GetSkillDuration(skill) ?? 0f;

        controlled.BusyUntil = Clock.Time + Wait(Math.Max(length, hits.Count > 0 ? hits[^1].Time : 0f));

        var (place, yaw) = PlaceOf(member, controlled);
        var position = new SkillPositionValue { Position = place, Angle = yaw, MotionClass = ControlNoMotion };

        creatureManager.Broadcast(new SkillActiveBroadcastResponse
        {
            Skill = default(ActiveSkillValue) with
            {
                Actor = controlled.Actor,
                Skill = skill,
                Position = position,
                RandomTrapIndex = creatureManager.Random.Next(1, int.MaxValue)
            }
        });

        foreach (var hit in hits)
        {
            _schedule.Schedule(Wait(hit.Time), () => ControlStrike(member, controlled, skill, hit.Trigger, position));
        }

        return ClaimVerdict.Taken;
    }

    /// <summary>
    /// One blow of the controlled monster, from where it stands now. A monster let go strikes no more.
    /// </summary>
    private void ControlStrike(Fighter member, Controlled controlled, int skill, AttackTrigger trigger, SkillPositionValue position)
    {
        if (Closed || member.Gone || !_controlled.TryGetValue(member, out var now) || now != controlled ||
            !creatureManager.Vitals.IsAlive(member.Player))
        {
            return;
        }

        var (place, yaw) = PlaceOf(member, controlled);
        var area = new AttackArea(trigger, place, yaw);
        var targets = creatureManager.FindMonsters((at, body) => area.Touches(at, body.BodyRadius, body.BodyHeight));

        Run(ApplyDamageAsync(member, skill, trigger, targets, position, origin: place, card: new CardStrike(controlled.Actor, place, yaw)));
    }

    private (Vector3 Place, float Yaw) PlaceOf(Fighter member, Controlled controlled)
    {
        var owner = Ecs.Get<WorldComponent>(member.Player);

        return (controlled.Follows ? owner.Position : controlled.Place, owner.Rotation);
    }
}
