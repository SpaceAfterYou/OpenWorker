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
    /// The widest a controlled monster is taken to be against walls when its skill carries it.
    /// </summary>
    private const float ControlCarryRadius = 100f;

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

        /// <summary>
        /// The skill it plays now.
        /// </summary>
        public ControlPlay? Play { get; set; }
    }

    /// <summary>
    /// A skill the controlled monster plays: it starts where the key found it, and the moves of its animations carry it the way it
    /// faces, short of walls.
    /// </summary>
    private sealed class ControlPlay(int skill, SkillTimeline timeline, SkillPositionValue position, float radius)
    {
        private int _move;
        private bool _stopped;

        public int Skill { get; } = skill;
        public SkillTimeline Timeline { get; } = timeline;
        public SkillPositionValue Position { get; } = position;
        public Vector3 Place { get; private set; } = position.Position;

        /// <summary>
        /// Carries it by the moves due up to this moment of the skill; a wall ends the carry for the rest of the skill.
        /// </summary>
        public Vector3 CarryTo(float time, INavigation navigation)
        {
            var offset = Vector2.Zero;

            for (; _move < Timeline.Moves.Count && Timeline.Moves[_move].Time <= time; _move++)
            {
                offset += new Vector2(Timeline.Moves[_move].Offset.X, Timeline.Moves[_move].Offset.Y);
            }

            if (_stopped || offset == Vector2.Zero)
            {
                return Place;
            }

            var (sin, cos) = MathF.SinCos(Position.Angle * (MathF.PI / 180f));
            var wanted = Place + new Vector3(offset.X * cos - offset.Y * sin, offset.X * sin + offset.Y * cos, 0f);

            Place = navigation.Clamp(Place, wanted, radius, out var reached);
            _stopped = !reached;

            return Place;
        }
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
        var timeline = actions.GetTimeline(skill, controlled.Behavior.ActionFile);
        var length = timeline.Length > 0f ? timeline.Length : creatureManager.Services?.Behaviors.GetSkillDuration(skill) ?? 0f;

        controlled.BusyUntil = Clock.Time + Wait(length);

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

        var play = new ControlPlay(skill, timeline, position, Math.Min(combat.GetMonster(controlled.Behavior.Id).BodyRadius, ControlCarryRadius));

        controlled.Play = play;

        if (timeline.Hits.Count > 0)
        {
            _schedule.Schedule(Wait(timeline.Hits[0].Time), () => ControlStrike(member, controlled, play, 0));
        }

        return ClaimVerdict.Taken;
    }

    /// <summary>
    /// One blow of the controlled monster, at the moment the clients play it and from where its skill has carried it by then. A blow
    /// that lands holds the skill for its freeze, so what is still to come comes that much later. A monster let go, or one that
    /// began another skill, strikes no more for this one.
    /// </summary>
    private void ControlStrike(Fighter member, Controlled controlled, ControlPlay play, int index)
    {
        if (Closed || member.Gone || !_controlled.TryGetValue(member, out var now) || now != controlled || controlled.Play != play ||
            !creatureManager.Vitals.IsAlive(member.Player))
        {
            return;
        }

        var hit = play.Timeline.Hits[index];
        var place = play.CarryTo(hit.Time, creatureManager.Navigation);
        var yaw = play.Position.Angle;
        var area = new AttackArea(hit.Trigger, place, yaw);
        var targets = creatureManager.FindMonsters((at, body) => area.Touches(at, body.BodyRadius, body.BodyHeight));

        Logger.ControlledStrike((int)controlled.Actor, play.Skill, hit.Trigger.Id, hit.Trigger.RangeType, hit.Trigger.Radius, hit.Trigger.Angle, hit.Trigger.Center,
            hit.Trigger.Height, place, yaw, targets.Length);

        var freeze = targets.Length > 0 && float.IsFinite(hit.Trigger.Reaction.HitFreeze) ? Math.Max(hit.Trigger.Reaction.HitFreeze, 0f) : 0f;

        controlled.BusyUntil += Wait(freeze);

        if (index + 1 < play.Timeline.Hits.Count)
        {
            _schedule.Schedule(Wait(play.Timeline.Hits[index + 1].Time - hit.Time + freeze), () => ControlStrike(member, controlled, play, index + 1));
        }

        Run(ApplyDamageAsync(member, play.Skill, hit.Trigger, targets, play.Position, origin: place, card: new CardStrike(controlled.Actor, place, yaw)));
    }

    private (Vector3 Place, float Yaw) PlaceOf(Fighter member, Controlled controlled)
    {
        var owner = Ecs.Get<WorldComponent>(member.Player);

        return (controlled.Follows ? owner.Position : controlled.Place, owner.Rotation);
    }
}
