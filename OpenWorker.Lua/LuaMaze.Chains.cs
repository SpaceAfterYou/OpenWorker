using System.Numerics;
using Arch.Core;
using Lua;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Gameplay.Messages.Response.Person.Components;

namespace OpenWorker.Lua;

public partial class LuaMaze
{
    /// <summary>
    /// A chain between monsters, stepped with the maze while its skill lasts.
    /// </summary>
    private sealed class ChainRun(int skill, AttackTrigger trigger, ChainLightning chain, uint session, SkillPositionValue position, int generation, TimeSpan fired)
    {
        public int Skill { get; } = skill;
        public AttackTrigger Trigger { get; } = trigger;
        public ChainLightning Chain { get; } = chain;
        public uint Session { get; } = session;
        public SkillPositionValue Position { get; } = position;
        public int Generation { get; } = generation;
        public TimeSpan Last { get; set; } = fired;
        public List<ChainHit> Hits { get; } = [];
        public List<ChainChange> Changes { get; } = [];

        /// <summary>
        /// Its step failed, and it holds nobody any more.
        /// </summary>
        public bool Failed { get; set; }
    }

    /// <summary>
    /// The client lets a chain go when its animation gets there. The chain jumps between monsters on the server,
    /// and the client draws it from what it is told: who it holds and when it lets go. As for a projectile, a player who is down
    /// lets none go, and a skill no more than its animations let go.
    /// </summary>
    [LuaIgnoreMember]
    public ClaimVerdict FireChain(Entity player, SkillProjectileValue request)
    {
        if (!Started || Find(player) is not { } member || !creatureManager.Vitals.IsAlive(player))
        {
            return ClaimVerdict.Dropped;
        }

        var hero = Ecs.Get<PersonInfoComponent>(player).Hero;

        // Only the skill that is being played may let a chain go.
        if (member.Playback is not { } run || run.Skill != request.Skill ||
            !actions.TryGetChain(request.Skill, hero, request.TriggerIndex, out var trigger))
        {
            return ClaimVerdict.Dropped;
        }

        if (!TakeShot(member, run, trigger.Id))
        {
            return Refuse(member);
        }

        var attacker = Ecs.Get<WorldComponent>(player);
        var chain = ChainLightning.Fire(trigger, attacker.Position, attacker.Rotation, creatureManager.GetFlightTargets());
        var session = ++member.ProjectileSession;
        var first = chain.Targets.Count > 0 ? new ActorValue(chain.Targets[0], ActorType.Monster) : (ActorValue)(-1);

        creatureManager.Broadcast(new SkillChainBroadcastResponse
        {
            Owner = Ecs.Get<ActorComponent>(player),
            SkillChainPosition = default(SkillChainPosition) with
            {
                Skill = request.Skill,
                TriggerIndex = request.TriggerIndex,
                Position = attacker.Position,
                Direction = new Vector3(0f, -1f, 0f) * actions.GetMaxRange(request.Skill),
                Session = session,
                Target = first
            }
        });

        var position = default(SkillPositionValue) with { Position = attacker.Position, Angle = attacker.Rotation };

        member.Chains.Add(new ChainRun(request.Skill, trigger, chain, session, position, member.SkillGeneration, Clock.Time));

        return ClaimVerdict.Taken;
    }

    /// <summary>
    /// The chains of the player step with the maze. A chain belongs to the skill that let it go: it lives its time, or
    /// until the player starts another skill.
    /// </summary>
    private void StepChains(Fighter member)
    {
        var chains = member.Chains;

        if (chains.Count == 0)
        {
            return;
        }

        var now = Clock.Time;

        for (var index = 0; index < chains.Count && !Closed && !member.Gone; index++)
        {
            var run = chains[index];

            if (run.Failed || run.Chain.IsOver || run.Generation != member.SkillGeneration)
            {
                continue;
            }

            try
            {
                var seconds = (float)Math.Max(0d, (now - run.Last).TotalSeconds);
                var owner = Ecs.Get<WorldComponent>(member.Player);

                run.Last = now;
                run.Hits.Clear();
                run.Changes.Clear();

                run.Chain.Advance(seconds, creatureManager.GetFlightTargets(), owner.Position, owner.Rotation, run.Hits, run.Changes);

                foreach (var change in run.Changes)
                {
                    creatureManager.Broadcast(new SkillChainUpdateTargetResponse
                    {
                        Session = run.Session,
                        IsAdd = change.Added,
                        Target = new ActorValue(change.Target, ActorType.Monster)
                    });
                }

                if (run.Hits.Count > 0)
                {
                    Run(StrikeChainAsync(member, run.Skill, run.Trigger, run.Hits, run.Position));
                }
            }

            catch (Exception e)
            {
                Logger.ChainFailed(e);

                run.Failed = true;
            }
        }

        chains.RemoveAll(e => e.Failed || e.Chain.IsOver || e.Generation != member.SkillGeneration);
    }

    /// <summary>
    /// Everyone the chain reaches at one moment is hit together. The links after the first carry a share of the damage.
    /// </summary>
    private ValueTask StrikeChainAsync(Fighter member, int skill, AttackTrigger trigger, List<ChainHit> hits, SkillPositionValue position)
    {
        var targets = new List<Entity>();
        var shares = new List<float>();

        foreach (var hit in hits)
        {
            if (creatureManager.FindMonster(hit.Target) is { } monster)
            {
                targets.Add(monster);
                shares.Add(hit.Order > 0 ? trigger.Chain!.DamageMultiple : 1f);
            }
        }

        return ApplyDamageAsync(member, skill, trigger, [.. targets], position, [.. shares]);
    }
}
