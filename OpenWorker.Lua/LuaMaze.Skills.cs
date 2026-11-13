using Arch.Core;
using Lua;
using OpenWorker.Batch.Actions;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Lua.Buffs;

namespace OpenWorker.Lua;

public partial class LuaMaze
{
    /// <summary>
    /// A skill that is being played: the moments of its attacks are counted on the game time from its start.
    /// </summary>
    private sealed class SkillRun(int skill, SkillPlayback playback, SkillPositionValue position, int generation, TimeSpan began)
    {
        public int Skill { get; } = skill;
        public SkillPlayback Playback { get; } = playback;
        public SkillPositionValue Position { get; } = position;
        public int Generation { get; } = generation;
        public TimeSpan Began { get; } = began;

        /// <summary>
        /// The latest moment the playback was told; the clock can stand a moment behind a reading taken just before a tick.
        /// </summary>
        public float Last { get; set; }
    }

    /// <summary>
    /// The client only says that a skill has started, so the server decides what it hits and when.
    /// A new skill interrupts the previous one, along with the attacks it has not reached yet.
    /// </summary>
    [LuaIgnoreMember]
    public void StartSkill(Entity player, int skill, SkillPositionValue position)
    {
        if (!Started || Find(player) is not { } member)
        {
            return;
        }

        // The script hears of every skill the player uses.
        Run(State.OnActionSkill(skill, this));

        _buffs.OnCondition(player, MazeBuffs.OnSkill);

        Begin(member, skill, position, true);
    }

    /// <summary>
    /// Whether the player may start another skill now: the client lets a skill be cut only once its animation has come to the
    /// moment it may be, and a client that starts a skill again and again before that would fire its shots over and over.
    /// </summary>
    [LuaIgnoreMember]
    public bool MayStartSkill(Entity player)
    {
        if (Find(player) is not { Playback: { } run } member)
        {
            return true;
        }

        BringUp(member, run);

        return member.Playback != run || run.Playback.MayCut(run.Last);
    }

    /// <summary>
    /// The skill the player plays now, or zero when it plays none or the one it plays is about to end.
    /// </summary>
    [LuaIgnoreMember]
    public int PlayingSkill(Entity player)
    {
        if (Find(player) is not { Playback: { } run } member)
        {
            return 0;
        }

        BringUp(member, run);

        return member.Playback == run && !run.Playback.IsEnding(run.Last) ? run.Skill : 0;
    }

    /// <summary>
    /// Whether the skill is a move on the hero of the player: it plays an animation that attacks nothing (an evade, a roll, a
    /// dash). A move cuts the skill the player plays.
    /// </summary>
    [LuaIgnoreMember]
    public bool IsMove(Entity player, int skill)
    {
        return MoveOf(player, skill) is not null;
    }

    private ActionAnimation?[]? MoveOf(Entity player, int skill)
    {
        if (actions.GetAnimations(skill, Ecs.Get<PersonInfoComponent>(player).Hero) is not { } animations || (animations.Start is null && animations.End is null))
        {
            return null;
        }

        ActionAnimation?[] clips = [animations.Start, animations.Loop, animations.Extra, animations.End];

        return clips.All(e => e is null || e.Attacks.Count == 0) ? clips : null;
    }

    /// <summary>
    /// The player evades, rolls or dashes: the skill it plays ends there (with its attacks to come, its shots and its chains).
    /// A move during which the client holds back the skills of the player until a moment of its animation plays in its place,
    /// until it is over or the client says it ended; during the others (a dash) the player may start a skill at once. A skill
    /// that is no move changes nothing.
    /// </summary>
    [LuaIgnoreMember]
    public void Evade(Entity player, int skill)
    {
        if (!Started || Find(player) is not { } member || MoveOf(player, skill) is not { } clips)
        {
            return;
        }

        if (clips.Any(e => e?.Cuts is { Count: > 0 }))
        {
            Begin(member, skill, default, false);

            return;
        }

        member.SkillGeneration++;
        member.CurrentSkill = skill;
        member.Playback = null;
        member.SkillWake.Cancel();
    }

    /// <summary>
    /// The evade or the dash of the player is over.
    /// </summary>
    [LuaIgnoreMember]
    public void EndEvade(Entity player, int skill)
    {
        if (Find(player) is { Playback: { } run } member && run.Skill == skill)
        {
            member.SkillGeneration++;
            member.Playback = null;
            member.SkillWake.Cancel();
        }
    }

    /// <summary>
    /// A new skill interrupts the previous one, along with the attacks it has not reached yet. The akashic skill with a called
    /// card plays the card's coming instead of its own attacks, when <paramref name="callsCards"/>.
    /// </summary>
    private void Begin(Fighter member, int skill, SkillPositionValue position, bool callsCards)
    {
        var generation = ++member.SkillGeneration;
        var hero = Ecs.Get<PersonInfoComponent>(member.Player).Hero;

        member.CurrentSkill = skill;
        member.Playback = null;
        member.SkillWake.Cancel();

        if (callsCards && BringsCardIn(member, skill, generation))
        {
            return;
        }

        if (actions.GetAnimations(skill, hero) is not { } animations)
        {
            return;
        }

        var run = new SkillRun(skill, new SkillPlayback(animations, AttackSpeedOf(member.Player)), position, generation, Clock.Time);

        member.Playback = run;
        member.PressedKeys.Clear();

        PlanCarries(member, skill, hero, generation, run.Began);
        Play(member, run);
    }

    /// <summary>
    /// The attack speed every hero starts with; the client plays the animations of a skill at the player's share of it.
    /// </summary>
    private const float NormalAttackSpeed = 100f;

    private float AttackSpeedOf(Entity player)
    {
        return Ecs.TryGet<AbilityComponent>(player, out var ability) && float.IsFinite(ability.Speed.Attack) && ability.Speed.Attack > 0f
            ? ability.Speed.Attack / NormalAttackSpeed
            : 1f;
    }

    /// <summary>
    /// Brings the playback of a skill up to now: what is due lands, and the step that has begun is the one that plays.
    /// </summary>
    private void BringUp(Fighter member, SkillRun run)
    {
        member.SkillWake.Cancel();
        Play(member, run);
    }

    /// <summary>
    /// The player holds the attack key or lets it go. What it means depends on the skill that is being played.
    /// </summary>
    [LuaIgnoreMember]
    public void PressKey(Entity player, bool pressed)
    {
        if (Find(player) is { Playback: { Playback.IsDone: false } run } member)
        {
            member.PressedKeys.Enqueue(pressed);
            member.SkillWake.Cancel();

            // The key cuts the step short now, not at the next tick.
            Play(member, run);
        }
    }

    /// <summary>
    /// Follows a skill through its steps: each attack is judged at its moment, and the key can cut a step short. It
    /// runs when the skill starts, when the key changes and at the tick of the next moment of the skill, so an attack
    /// lands at the first tick at or after its moment.
    /// </summary>
    private void Play(Fighter member, SkillRun run)
    {
        try
        {
            var now = Math.Max(run.Last, (float)(Clock.Time - run.Began).TotalSeconds);

            run.Last = now;

            while (!Closed && !member.Gone && run.Generation == member.SkillGeneration && run.Playback.NextTime is { } next)
            {
                if (next > now && member.PressedKeys.Count == 0)
                {
                    member.SkillWake = _schedule.ScheduleAt(run.Began + Wait(next), () => Play(member, run));

                    return;
                }

                foreach (var hit in run.Playback.Advance(now))
                {
                    if (hit.Trigger.IsTrap)
                    {
                        PlaceTrap(member, run.Skill, hit.Trigger, run.Position);
                    }
                    else
                    {
                        Run(ApplyHitAsync(member, run, hit));
                    }
                }

                while (member.PressedKeys.TryDequeue(out var pressed))
                {
                    run.Playback.Press(pressed, now);
                }
            }
        }

        catch (Exception e)
        {
            Logger.SkillPlaybackFailed(e);
        }
    }

    private ValueTask ApplyHitAsync(Fighter member, SkillRun run, SkillHit hit)
    {
        var attacker = Ecs.Get<WorldComponent>(member.Player);
        var area = new AttackArea(hit.Trigger, attacker.Position, attacker.Rotation);
        var targets = creatureManager.FindMonsters((place, body) => area.Touches(place, body.BodyRadius, body.BodyHeight));

        // A blow that lands holds the animation of the attacker for a moment, on its client as here.
        if (targets.Length > 0)
        {
            run.Playback.Pause(hit.Trigger.Reaction.HitFreeze);
        }

        return ApplyDamageAsync(member, run.Skill, hit.Trigger, targets, run.Position);
    }

    /// <summary>
    /// A claim of the client that cannot be made counts against its error budget once in this time: the ones it sent before the
    /// first was refused are the same fault.
    /// </summary>
    private static readonly TimeSpan RefusalInterval = TimeSpan.FromSeconds(1);

    private ClaimVerdict Refuse(Fighter member)
    {
        if (member.RefusedAt is { } last && Clock.Time - last < RefusalInterval)
        {
            return ClaimVerdict.Dropped;
        }

        member.RefusedAt = Clock.Time;

        return ClaimVerdict.Refused;
    }

    /// <summary>
    /// The skill being played lets the client fire one more shot of the attack: its animation has let go of one the client has
    /// not fired yet, or is about to.
    /// </summary>
    private bool TakeShot(Fighter member, SkillRun run, int attack)
    {
        // The playback is brought up to now first: the shot may be of the animation that has just begun.
        member.SkillWake.Cancel();
        Play(member, run);

        return member.Playback == run && run.Playback.TakeShot(attack, run.Last);
    }
}
