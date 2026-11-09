using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// When each skill of a player can be used again: by cooldown group, and the short global cooldown every skill starts.
/// </summary>
[EntityComponent(EntityComponentService.District)]
public readonly record struct SkillCooldownComponent
{
    /// <summary>
    /// The moment each cooldown group is over, on the clock the fight is counted on. Made on the first skill.
    /// </summary>
    public Dictionary<int, TimeSpan>? Groups { get; init; }

    public TimeSpan Global { get; init; }
}

/// <summary>
/// Why a skill is refused, with the codes the client shows (the answer to 06/08 carries one).
/// </summary>
public static class SkillRefusal
{
    public const ushort None = 0;

    /// <summary>The skill is not in the table, not learned, or not in the deck.</summary>
    public const ushort NotOwned = 56001;

    public const ushort OtherHero = 56002;

    public const ushort LevelTooLow = 56003;

    public const ushort NoHealth = 56004;

    public const ushort NoSoulGauge = 56005;

    public const ushort NoStamina = 56006;

    public const ushort NoSoulVapor = 56007;

    /// <summary>The player cannot attack now (dead).</summary>
    public const ushort CannotAttack = 56009;

    public const ushort Cooldown = 56010;

    /// <summary>Every refusal of learning a skill.</summary>
    public const ushort CannotLearn = 56012;
}

/// <summary>
/// Decides whether a player may start a skill and takes what the skill costs, in the order the original server checks:
/// the skill is the player's own and in its deck, the level, the hero, the cost, the cooldown. Using it takes the cost
/// and starts the cooldown of its group and the global cooldown.
/// </summary>
public sealed class SkillUse(World world, ReadOnlyCollection<SkillRow> skills, PlayerGauges gauges)
{
    /// <summary>
    /// The skill every hero has without learning it (the akashic records).
    /// </summary>
    private const int CommonSkill = 30000;

    /// <summary>
    /// The place of use of the plain attacks every hero has.
    /// </summary>
    private const byte BasicPosition = 2;

    private const byte PassiveSkill = 1;
    private const byte ActiveSkill = 2;

    /// <summary>
    /// The kinds of skills that are never put in a deck: the moves of modes, transformations and the like.
    /// </summary>
    private static readonly byte[] DecklessKinds = [4, 5, 6, 7, 9];

    /// <summary>
    /// The state of use of a skill that can be used in any state, which is never put in a deck either.
    /// </summary>
    private const byte AnyState = 1;

    private const int DeckPositions = 6;

    private readonly RowIndex<int, SkillRow> _skills = skills.IndexBy(e => e.Id);

    public bool TryGetSkill(int skill, out SkillRow row)
    {
        return _skills.TryGet(skill, out row);
    }

    /// <summary>
    /// Whether the player may start the skill now; <see cref="SkillRefusal.None"/> when it may.
    /// </summary>
    public ushort Check(Entity player, int skill, TimeSpan now)
    {
        if (!_skills.TryGet(skill, out var row))
        {
            return SkillRefusal.NotOwned;
        }

        if (!IsOwned(player, row) || !IsInDeck(player, row))
        {
            return SkillRefusal.NotOwned;
        }

        var level = world.TryGet<LevelComponent>(player, out var reached) ? reached.Level : (byte)1;

        if (row.RequiredLevel > level)
        {
            return SkillRefusal.LevelTooLow;
        }

        if (row.UseClass != 0 && world.TryGet<PersonInfoComponent>(player, out var person) && row.UseClass != (byte)person.Hero)
        {
            return SkillRefusal.OtherHero;
        }

        if (world.TryGet<AbilityComponent>(player, out var ability) && ability.Health.Current <= 0)
        {
            return SkillRefusal.CannotAttack;
        }

        var kind = (SkillCostKind)row.CostAttribute;

        if (!gauges.CanPay(player, kind, Cost(player, row)))
        {
            return kind switch
            {
                SkillCostKind.Health => SkillRefusal.NoHealth,
                SkillCostKind.SoulGauge => SkillRefusal.NoSoulGauge,
                SkillCostKind.Stamina => SkillRefusal.NoStamina,
                _ => SkillRefusal.NoSoulVapor
            };
        }

        return CooldownLeft(player, row, now) > TimeSpan.Zero ? SkillRefusal.Cooldown : SkillRefusal.None;
    }

    /// <summary>
    /// Takes the cost of a skill that was allowed and starts its cooldowns.
    /// </summary>
    public void Use(Entity player, int skill, TimeSpan now)
    {
        if (!_skills.TryGet(skill, out var row))
        {
            return;
        }

        gauges.Pay(player, (SkillCostKind)row.CostAttribute, Cost(player, row));
        gauges.Rest(player, row.SkipPriorityGroup);

        StartCooldown(player, row, now);
    }

    /// <summary>
    /// What the skill costs the player now: the cost of the table, or a share in percent of the current value for a cost of
    /// type 2. A cost under one point is one point.
    /// </summary>
    public float Cost(Entity player, in SkillRow row)
    {
        float cost = row.Cost;

        if (row.CostType == 2)
        {
            var current = (SkillCostKind)row.CostAttribute switch
            {
                SkillCostKind.SoulGauge => gauges.Of(player).SoulGauge,
                SkillCostKind.Stamina => gauges.Of(player).Stamina,
                SkillCostKind.Health => world.TryGet<AbilityComponent>(player, out var ability) ? ability.Health.Current : 0,
                _ => 0f
            };

            cost = current * cost * 0.01f;
        }

        return cost is > 0f and < 1f ? 1f : cost;
    }

    /// <summary>
    /// How long until the player may use the skill again: the cooldown of its group, or the global cooldown when the skill has one.
    /// </summary>
    public TimeSpan CooldownLeft(Entity player, in SkillRow row, TimeSpan now)
    {
        if (!world.TryGet<SkillCooldownComponent>(player, out var cooldowns))
        {
            return TimeSpan.Zero;
        }

        if (cooldowns.Groups is { } groups && groups.TryGetValue(row.CooldownGroup, out var end) && end > now)
        {
            return end - now;
        }

        return row.GlobalCooldown > 0 && cooldowns.Global > now ? cooldowns.Global - now : TimeSpan.Zero;
    }

    private void StartCooldown(Entity player, in SkillRow row, TimeSpan now)
    {
        // A passive skill without a cooldown of its own starts none.
        if ((row.Cooldown <= 0 && row.GlobalCooldown <= 0) || (row.SkillType == PassiveSkill && row.Cooldown <= 0))
        {
            return;
        }

        var cooldowns = world.TryGet<SkillCooldownComponent>(player, out var existing) ? existing : default;
        var groups = cooldowns.Groups ?? [];
        var end = now + TimeSpan.FromMilliseconds(Math.Max(0, row.Cooldown));

        if (!groups.TryGetValue(row.CooldownGroup, out var running) || end > running)
        {
            groups[row.CooldownGroup] = end;
        }

        var global = cooldowns.Global;

        if (row.SkillType != PassiveSkill && row.GlobalCooldown > 0)
        {
            var globalEnd = now + TimeSpan.FromMilliseconds(row.GlobalCooldown);

            if (globalEnd > global)
            {
                global = globalEnd;
            }
        }

        world.Set(player, new SkillCooldownComponent { Groups = groups, Global = global });
    }

    /// <summary>
    /// The plain attacks, the common skill and the moves every hero has are owned without learning; any other skill must be
    /// learned, itself or another level of its group with the same divergence.
    /// </summary>
    private bool IsOwned(Entity player, in SkillRow row)
    {
        if (row.UsePosition == BasicPosition || row.Id == CommonSkill)
        {
            return true;
        }

        if (world.TryGet<SkillLearnedComponent>(player, out var learned) && learned.Values is { } values)
        {
            foreach (var skill in values)
            {
                if (skill.Skill == row.Id)
                {
                    return true;
                }
            }

            foreach (var skill in values)
            {
                if (skill.Skill != 0 && _skills.TryGet(skill.Skill, out var own) && own.Group == row.Group &&
                    ((row.DivergenceGroup1 != 0 && own.DivergenceGroup1 == row.DivergenceGroup1) ||
                     (row.DivergenceGroup2 != 0 && own.DivergenceGroup2 == row.DivergenceGroup2)))
                {
                    return true;
                }
            }
        }

        return row.SkillType is PassiveSkill or ActiveSkill && row.PassiveType != 0;
    }

    /// <summary>
    /// An active skill is used from the deck; the plain attacks, the moves and the kinds that never go in a deck are not.
    /// </summary>
    private bool IsInDeck(Entity player, in SkillRow row)
    {
        if (row.UsePosition == BasicPosition || row.Id == CommonSkill ||
            (row.SkillType is PassiveSkill or ActiveSkill && row.PassiveType != 0) ||
            Array.IndexOf(DecklessKinds, row.SkillType) >= 0 || row.UseState == AnyState)
        {
            return true;
        }

        if (!world.TryGet<SkillDeckComponent>(player, out var deck) || deck.SlotList is not { } decks)
        {
            return false;
        }

        foreach (var slot in decks)
        {
            if (slot.Position >= DeckPositions * 5)
            {
                continue;
            }

            foreach (var skill in slot.GetList())
            {
                if (skill != 0 && (skill == row.Id || (row.SwapSkill != 0 && skill == row.SwapSkill)))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
