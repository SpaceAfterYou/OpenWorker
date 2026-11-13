using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Buffs;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.HiddenEvents;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot.Modules.Skill;
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
/// What the bonus a deck gives does, by the type column of the deck bonus table.
/// </summary>
public enum DeckBonusKind : byte
{
    Damage = 0,
    Cooldown = 1,
    SoulCost = 2
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

    /// <summary>The player cannot attack now: dead, or stunned, paralysed, asleep or frozen by a buff.</summary>
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
public sealed class SkillUse(World world, ReadOnlyCollection<SkillRow> skills, PlayerGauges gauges, ReadOnlyCollection<DeckBonusRow>? deckBonuses = null, CombatTables? combat = null,
    HiddenEventCounter? hiddenEvents = null)
{
    private readonly RowIndex<short, DeckBonusRow> _deckBonuses = (deckBonuses ?? new ReadOnlyCollection<DeckBonusRow>([])).IndexBy(e => e.Id);

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
    /// The divergence the player has chosen for the skill (or for the learned level of its group), zero for a skill without
    /// one and for the skills nobody learns. The answer to a skill carries it: the client looks the skill it plays up by
    /// it, and the number in the request is not to be trusted (the client leaves it unset for a plain attack). A skill
    /// without one of its own takes the one of its parent, as the original does.
    /// </summary>
    public int DivergenceOf(Entity player, int skill, int parent = 0)
    {
        // The skill itself first, then the skill it continues (a combo step names its first skill as its parent).
        var own = DivergenceOfSkill(player, skill);

        return own != 0 || parent == 0 ? own : DivergenceOfSkill(player, parent);
    }

    private int DivergenceOfSkill(Entity player, int skill)
    {
        if (!world.TryGet<SkillLearnedComponent>(player, out var learned) || learned.Values is not { } values)
        {
            return 0;
        }

        foreach (var own in values)
        {
            if (own.Skill == skill)
            {
                return own.Divergence;
            }
        }

        if (!_skills.TryGet(skill, out var row))
        {
            return 0;
        }

        foreach (var own in values)
        {
            if (own.Skill != 0 && _skills.TryGet(own.Skill, out var ownRow) && ownRow.Group == row.Group)
            {
                return own.Divergence;
            }
        }

        return 0;
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

        // A buff that holds the player keeps it from attacking.
        if (BuffBook.Of(world, player) is { IsHeld: true })
        {
            return SkillRefusal.CannotAttack;
        }

        // A player thrown into the air cannot start a skill that is not for the air.
        if (world.TryGet<PlayerPostureComponent>(player, out var posture) && posture.DownUntil > now && !SkillStates.CanStartInTheAir(row.UseState, row.NormalState))
        {
            return SkillStates.Refusal(row.UseState, row.NormalState);
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

        var kind = (SkillCostKind)row.CostAttribute;
        var cost = Cost(player, row);

        // A buff can make the skills cost no soul gauge, though the player must still have it.
        if (kind != SkillCostKind.SoulGauge || BuffBook.Of(world, player) is not { FreeSoulGauge: true })
        {
            gauges.Pay(player, kind, cost);
        }

        if (kind == SkillCostKind.Stamina)
        {
            hiddenEvents?.Count(player, HiddenEventTarget.Stamina, 0, (int)cost);
        }

        gauges.Rest(player, row.SkipPriorityGroup);

        StartCooldown(player, row, now);
    }

    /// <summary>
    /// What the skill costs the player now: the cost of the table, or a share in percent of the current value for a cost of
    /// type 2. A cost under one point is one point. A soul gauge cost is lower by the soul cost reductions of the buffs and the
    /// deck bonus, a stamina cost by the stamina reduction of the buffs.
    /// </summary>
    public float Cost(Entity player, in SkillRow row)
    {
        float cost = row.Cost;
        var buffs = BuffBook.Of(world, player);
        var kind = (SkillCostKind)row.CostAttribute;

        if (kind == SkillCostKind.SoulGauge)
        {
            var reduction = (buffs?.SoulCostDown ?? 0f) + DeckBonus(player, DeckBonusKind.SoulCost) + Special(player).Of(SpecialEffect.SoulCostDown) * 0.01f;

            cost -= cost * reduction;
        }

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

            if (cost is > 0f and < 1f)
            {
                cost = 1f;
            }
        }

        if (kind == SkillCostKind.Stamina && buffs is { StaminaCostDown: > 0f } book)
        {
            cost -= cost * book.StaminaCostDown;
        }

        return cost is > 0f and < 1f ? 1f : Math.Max(0f, cost);
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
        var end = now + TimeSpan.FromMilliseconds(Math.Max(0, row.Cooldown) * (1f - CooldownRate(player, row) * 0.01f));

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
    /// How much shorter the cooldown of a skill is, in percent from 0 to 100: the buffs of the player and the cooldown bonus of
    /// the deck place the skill was linked from. A passive skill keeps its whole cooldown.
    /// </summary>
    public float CooldownRate(Entity player, in SkillRow row)
    {
        if (row.SkillType == PassiveSkill)
        {
            return 0f;
        }

        var rate = (BuffBook.Of(world, player)?.CooldownRate ?? 0f) + Special(player).Of(SpecialEffect.CooldownShare) + DeckBonus(player, DeckBonusKind.Cooldown) * 100f;

        return Math.Clamp(rate, 0f, 100f);
    }

    /// <summary>
    /// The special effects of the gear, titles and buffs of the player.
    /// </summary>
    private SpecialEffects Special(Entity player)
    {
        return combat is null ? SpecialEffects.None : combat.GetPlayer(world, player).Special;
    }

    /// <summary>
    /// The player starts a skill from a row of its deck: the bonus of the place the skill holds there is the bonus of this skill,
    /// for its cost, its cooldown and its blows, until the next one starts. A skill at the first place, or outside the row, has
    /// none.
    /// </summary>
    public void Link(Entity player, int skill, byte row)
    {
        var link = default(SkillDeckLinkComponent);

        if (world.TryGet<SkillDeckComponent>(player, out var deck) && world.TryGet<SkillDeckBonusComponent>(player, out var chosen) &&
            DeckLink.BonusOf(deck.SlotList, chosen.Values, row, skill, _skills.TryGet(skill, out var own) ? own.SwapSkill : 0) is var bonus and > 0 and <= (ushort)short.MaxValue &&
            _deckBonuses.TryGet((short)bonus, out var found))
        {
            link = new SkillDeckLinkComponent((DeckBonusKind)found.BonusType, found.BonusValue);
        }

        if (world.Has<SkillDeckLinkComponent>(player))
        {
            world.Set(player, link);
        }
        else
        {
            world.Add(player, link);
        }
    }

    /// <summary>
    /// The bonuses the player may keep for the places of its deck page: each must be a row of the table for that place, of a
    /// level the player has reached. Any other is none.
    /// </summary>
    public ushort[] ChooseDeckBonus(Entity player, ushort[] requested)
    {
        var level = world.TryGet<LevelComponent>(player, out var reached) ? reached.Level : (byte)1;
        var kept = new ushort[SkillModuleDefines.SkillDeckBonusCount];

        for (var place = 0; place < kept.Length && place < requested.Length; place++)
        {
            if (requested[place] is var bonus and > 0 and <= (ushort)short.MaxValue && _deckBonuses.TryGet((short)bonus, out var row) &&
                row.Phase == place && row.RequiredLevel <= level)
            {
                kept[place] = bonus;
            }
        }

        return kept;
    }

    /// <summary>
    /// The value of the deck bonus of the skill the player started last, when it is of the kind asked for.
    /// </summary>
    private float DeckBonus(Entity player, DeckBonusKind kind)
    {
        return world.TryGet<SkillDeckLinkComponent>(player, out var link) && link.Kind == kind ? link.Value : 0f;
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
        return IsDeckless(row) || StandsInDeck(player, row);
    }

    private static bool IsDeckless(in SkillRow row)
    {
        return row.UsePosition == BasicPosition || row.Id == CommonSkill ||
               (row.SkillType is PassiveSkill or ActiveSkill && row.PassiveType != 0) ||
               Array.IndexOf(DecklessKinds, row.SkillType) >= 0 || row.UseState == AnyState;
    }

    private bool StandsInDeck(Entity player, in SkillRow row)
    {
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
