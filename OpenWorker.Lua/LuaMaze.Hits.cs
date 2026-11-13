using System.Numerics;
using Arch.Core;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Modules.HiddenEvents;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Lua.Ai;
using OpenWorker.Gameplay.Ai;
using OpenWorker.Lua.Buffs;
using OpenWorker.Lua.Managers;

namespace OpenWorker.Lua;

public partial class LuaMaze
{
    // A monster that counts hits takes them no faster than this.
    private static readonly TimeSpan CountedHitGap = TimeSpan.FromMilliseconds(500);

    /// <param name="shares">The share of the damage each target takes, by their place in the list. Everyone takes all of it without.</param>
    /// <param name="origin">Where the blow comes from when it is not the player: the place a shot burst at or a trap lies.</param>
    /// <param name="card">The card of the player that strikes, when it is not the player: the hit is shown as the card's.</param>
    /// <param name="strength">The stats the blow is dealt with when they are not the player's: those of the weapon it fires.</param>
    private async ValueTask ApplyDamageAsync(Fighter member, int skill, AttackTrigger trigger, Entity[] targets, SkillPositionValue position, float[]? shares = null, Vector3? origin = null,
        CardStrike? card = null, CombatStats? strength = null)
    {
        var player = member.Player;

        // An attack for the side of the player, or one that only gives a buff, hurts nobody.
        if (GivesBuffOnly(member, trigger, targets))
        {
            return;
        }

        // A hit that was on its way when the player left lands on nothing.
        if (targets.Length == 0 || !trigger.AffectsTargets || member.Gone || !Ecs.IsAlive(player))
        {
            return;
        }

        var attacker = Ecs.Get<WorldComponent>(player);

        var attackerStats = combat.GetPlayer(Ecs, player);
        var skillDamage = combat.GetSkill(skill);
        var dealt = strength ?? attackerStats;
        var deck = card is null ? DeckLink.DamageShare(Ecs, player) : 0f;
        var hits = targets.Select((target, i) => ApplyDamage(player, target, dealt, skillDamage, trigger.DamageRate * (shares?[i] ?? 1f), trigger, skill, HitKinds.Of(trigger), origin, deck)).ToArray();

        // The combo of the player goes into the message of the hit: the client shows it. The blows of a card do not count.
        var now = (long)Clock.Time.TotalMilliseconds;

        if (card is null)
        {
            member.Combo = Gameplay.Combat.Combo.Next(member.Combo, member.LastHit, hits.Length, now);
            member.LastHit = now;

            CountHiddenEvent(HiddenEventTarget.MaxCombo, 0, member.Combo);
        }

        MonstersHit?.Invoke(player, hits.Length, hits.Sum(e => (long)e.Damage));

        // The buffs of the player and of the monsters it hit that answer to a hit: blood, reflection, soul absorption, the cards
        // that wait for a hit, a critical hit or a miss, and the combo.
        for (var i = 0; i < targets.Length; i++)
        {
            if (hits[i].Damage > 0 && Ecs.IsAlive(targets[i]))
            {
                _buffs.AfterHit(player, targets[i], i, hits[i].Damage, Ecs.Get<MonsterCombatComponent>(targets[i]).Stats);
            }

            var flags = (HitResults)hits[i].DamageFlags;

            _buffs.OnCondition(player, flags.HasFlag(HitResults.Miss) ? MazeBuffs.OnMissedAttack : MazeBuffs.OnAttack);

            if (flags.HasFlag(HitResults.Critical))
            {
                _buffs.OnCondition(player, MazeBuffs.OnCriticalAttack);
            }
        }

        _buffs.OnHits(player, hits.Count(e => (e.DamageFlags & (byte)HitResults.Miss) == 0));

        // Every target hit gives the soul gauge of the attacker a share of what the skill absorbs; the player is told once.
        if (creatureManager.Services?.Gauges is { } gauges && skillDamage.SoulGaugeAbsorb > 0f)
        {
            var soul = gauges.Of(player).SoulGauge;
            var absorbed = 0f;

            foreach (var target in targets)
            {
                absorbed += SoulGauge.Absorbed(skillDamage.SoulGaugeAbsorb, Ecs.Get<MonsterCombatComponent>(target).Stats.SoulGaugeAbsorbRate, soul + absorbed, attackerStats.SoulGaugeMax);
            }

            gauges.Absorb(player, absorbed, attackerStats.SoulGaugeMax);
        }

        creatureManager.TellHitSlow(hits, trigger);

        creatureManager.Broadcast(new SkillActionBroadcastResponse
        {
            SkillDmgValueList = hits,
            SkillAction = default(SkillActionValue) with
            {
                SkillActorInfo = default(SkillActorInfoValue) with
                {
                    UxActorId = card?.Actor ?? Ecs.Get<ActorComponent>(player),
                    SkillPosInfo = default(SkillActionPosInfoValue) with
                    {
                        Position = card?.Place ?? attacker.Position,
                        Angle = card?.Yaw ?? attacker.Rotation,
                        MotionClass = position.MotionClass,
                        HasInputFlag = position.HasInputFlag
                    }
                },
                Skill = skill,
                TriggerIndex = (short)trigger.Id,
                ContinuousHit = card is null ? (ushort)Math.Min(member.Combo, ushort.MaxValue) : (ushort)0
            }
        });

        // Every blow that lands may wear the weapon of the player; the blows of a card do not.
        if (card is null && creatureManager.Services?.Wear is { } wear)
        {
            foreach (var hit in hits)
            {
                if (hit.Damage > 0)
                {
                    wear.Attacked(player);
                }
            }
        }

        // The buff of the attack reaches the targets it hit and left standing.
        _buffs.GiveFromHit(player, trigger, targets);

        // Another hit can land while a script runs, so the dead leave the fight before the first script starts.
        var dead = targets
            .Select((target, i) => (Target: target, Hit: hits[i]))
            .Where(e => Ecs.Get<CreatureComponent>(e.Target).Health == 0)
            .Select(e => (Removed: RemoveDead(e.Target), e.Hit))
            .ToArray();

        // What the player killed pays out at once; monsters that a script or the end of the maze takes away do not.
        foreach (var ((identifier, _, fell), _) in dead)
        {
            MonsterKilled?.Invoke(player, identifier, fell);
        }

        // Gear that gives life and soul gauge back for a kill does so for every monster killed.
        if (dead.Length > 0)
        {
            GiveBackOnKill(player, attackerStats, dead.Length);
        }

        var kind = actions.GetPassiveType(skill);

        foreach (var ((identifier, place, _), hit) in dead)
        {
            if (Closed)
            {
                return;
            }

            // The killer may have left while a script ran; the death still counts for the maze.
            await ReportKillAsync(player, identifier, MazeReporter.GetKillConditions(kind, (hit.DamageFlags & (byte)HitResults.Critical) != 0));

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

    private SkillDmgValue ApplyDamage(Entity attacker, Entity target, in CombatStats attackerStats, in SkillDamage skill, float attackRate, in AttackTrigger trigger, int skillId = 0, AiHitKind kind = AiHitKind.Skill, Vector3? origin = null,
        float deckShare = 0f)
    {
        // An armor whose time is up is whole for this hit: what the hit deals and how it is taken are read after that.
        creatureManager.RestoreSuperArmor(target);

        var creature = Ecs.Get<CreatureComponent>(target);
        var body = Ecs.Get<MonsterCombatComponent>(target);
        var defense = _buffs.DefenseOf(target, creatureManager.FrameDefenseOf(target, body.Defense));
        // A super armor that was broken before this hit lets half as much again through.
        var broken = body.MaxSuperArmor > 0f && body.SuperArmor <= 0f;
        var context = creatureManager.ContextOf(target);
        var result = DamageCalculator.Calculate(attackerStats, _buffs.StatsOf(target, body.Stats), skill, defense, attackRate, Random.Shared, broken, context);

        // The damage bonus of the deck place the skill was linked from.
        if (deckShare > 0f)
        {
            result = result with { Damage = result.Damage + (int)(result.Damage * deckShare) };
        }

        result = result with { Damage = _buffs.Adjust(attacker, target, result.Damage) };

        var lost = LifeTaken(target, result.Damage);

        var health = Math.Max(0, creature.Health - lost);

        Ecs.Set(target, creature with { Health = health });

        creatureManager.OnMonsterHit(target, attacker, result.Damage, skillId, kind);

        var armorShare = skill.SuperArmorAttack * (1f + attackerStats.Special.Of(SpecialEffect.SuperArmorDamage) * 0.01f);
        var effect = creatureManager.ReceiveBlow(target, BlowOf(attacker, trigger, origin), defense, result.Damage, armorShare, health > 0);

        CountHiddenEventBlow(attacker, target, skillId, body.SuperArmorBrokenUntil is not null, creature.Health > 0 && health == 0, context.Flying);

        return default(SkillDmgValue) with
        {
            UxActorId = Ecs.Get<ActorComponent>(target),
            ReactionType = (byte)trigger.ReactionType,
            DamageFlags = (byte)result.Flags,
            Damage = result.Damage,
            Health = health,
            ExtraMove = effect.ExtraMove,
            FlySpeed = effect.FlySpeed,
            DefenseType = effect.Defense,
            CurSuperArmorGauge = effect.SuperArmor
        };
    }

    /// <summary>
    /// The life a hit takes from a monster: the damage, or one point for a monster that counts hits, no sooner than the gap
    /// after the last one on the maze's clock.
    /// </summary>
    internal int LifeTaken(Entity target, int damage)
    {
        var body = Ecs.Get<MonsterCombatComponent>(target);

        if (!body.CountsHits)
        {
            return damage;
        }

        var now = Clock.Time;

        if (damage <= 0 || body.LastCountedHit is { } last && now - last < CountedHitGap)
        {
            return 0;
        }

        Ecs.Set(target, body with { LastCountedHit = now });

        return 1;
    }

    /// <summary>
    /// A kill gives a player life and soul gauge back by what its gear says.
    /// </summary>
    private void GiveBackOnKill(Entity player, in CombatStats stats, int kills)
    {
        var health = (int)(stats.Special.Of(SpecialEffect.HealthOnKill) * kills);

        if (health > 0 && creatureManager.Vitals.IsAlive(player))
        {
            creatureManager.Vitals.Heal(player, health);
        }

        var soul = stats.Special.Of(SpecialEffect.SoulGaugeOnKill) * kills;

        if (soul > 0f && creatureManager.Services?.Gauges is { } gauges)
        {
            gauges.Absorb(player, soul, stats.SoulGaugeMax);
        }
    }

    /// <summary>
    /// The blow of an attack event as it reaches a target: from a player, or from a helper, which is a monster.
    /// </summary>
    private Blow BlowOf(Entity attacker, in AttackTrigger trigger, Vector3? origin)
    {
        var place = Ecs.Get<WorldComponent>(attacker);
        var helper = Ecs.TryGet<MonsterCombatComponent>(attacker, out var combatant);

        return new Blow(
            trigger.Reaction,
            (ReactionKind)(byte)Math.Clamp(trigger.ReactionType, 0, byte.MaxValue),
            trigger.DamageRate,
            trigger.Angle,
            origin ?? place.Position,
            place.Position,
            place.Rotation,
            helper,
            helper ? combatant.WeightRank : PlayerVitals.WeightRank,
            IsMelee: trigger.HitsArea);
    }
}
