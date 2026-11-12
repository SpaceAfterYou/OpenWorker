using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// The card a player has called from the quick slots of its akashic records: the next akashic skill of the player brings
/// it into the fight, turned by the yaw the player had when it called it.
/// </summary>
public readonly record struct AkashicCallComponent(int Card, float Yaw);

/// <summary>
/// When each group of cards may be called again, and the short wait every called card starts for all of them. Cards keep
/// their own cooldowns, apart from those of the skills.
/// </summary>
public readonly record struct AkashicCooldownComponent(Dictionary<int, TimeSpan> Groups, TimeSpan Global);

/// <summary>
/// Why a call of a card is refused, with the codes the original answers 06/20 with.
/// </summary>
public static class AkashicRefusal
{
    public const ushort None = 0;

    /// <summary>
    /// No card of the book in the slot, a maze that bars cards, or a card the tables do not know.
    /// </summary>
    public const ushort NotUsable = 1;

    /// <summary>
    /// The group of the card, or every card, is still cooling down.
    /// </summary>
    public const ushort Cooldown = 2;

    /// <summary>
    /// The skill of the card is not in the table.
    /// </summary>
    public const ushort UnknownSkill = SkillRefusal.NotOwned;
}

/// <summary>
/// The cards a player calls into a fight from the five quick slots of its akashic record book, as the original handles them:
/// the card of the slot must be in the book and the maze must allow cards; a card that brings a creature in (types 1 and 2)
/// must have a skill the table knows and must not be cooling down, and then it is remembered for the next akashic skill of the
/// player and starts its cooldowns. Cards of the other kinds are answered and do nothing more.
/// </summary>
public sealed class AkashicCalls(
    World world,
    ReadOnlyCollection<AkashicRecordsRow> cards,
    ReadOnlyCollection<SkillRow> skills,
    ReadOnlyCollection<MazeRuleRow> rules,
    ReadOnlyCollection<CharacterInfoRow> heroes)
{
    /// <summary>
    /// The quick slots of the book a card is called from.
    /// </summary>
    public const int Slots = 5;

    /// <summary>
    /// The kind of skill that plays the akashic animation of a hero and brings the called card in.
    /// </summary>
    private const byte AkashicSkillType = 3;

    private const byte SummonCard = 1;
    private const byte SummonCardOther = 2;

    private readonly RowIndex<int, AkashicRecordsRow> _cards = cards.IndexBy(e => e.Id);
    private readonly RowIndex<int, SkillRow> _skills = skills.IndexBy(e => e.Id);
    private readonly RowIndex<ushort, MazeRuleRow> _rules = rules.IndexBy(e => e.Id);
    private readonly RowIndex<short, CharacterInfoRow> _heroes = heroes.IndexBy(e => e.Id);

    public bool TryGetCard(int card, out AkashicRecordsRow row) => _cards.TryGet(card, out row);

    /// <summary>
    /// Whether a card brings a creature into the fight when it is called.
    /// </summary>
    public static bool Summons(in AkashicRecordsRow card) => card.Type is SummonCard or SummonCardOther;

    /// <summary>
    /// The player calls the card of a quick slot in the given maze (0 for a place that is not a maze). On
    /// <see cref="AkashicRefusal.None"/> the card is the one of the slot.
    /// </summary>
    public ushort Call(Entity player, int slot, int maze, float yaw, TimeSpan now, out AkashicRecordsRow card)
    {
        card = default;

        if (CardOf(player, slot) is not { } called || !_cards.TryGet(called, out card))
        {
            return AkashicRefusal.NotUsable;
        }

        if (maze != 0 && _rules.TryGet((ushort)maze, out var rule) && rule.CardsBarred)
        {
            return AkashicRefusal.NotUsable;
        }

        if (!Summons(card))
        {
            return AkashicRefusal.None;
        }

        // The column holds the skill of the creature for a card that brings one in (the buff of a passive card).
        if (card.Buff == 0)
        {
            return AkashicRefusal.NotUsable;
        }

        if (!_skills.TryGet(card.Buff, out _))
        {
            return AkashicRefusal.UnknownSkill;
        }

        if (CooldownLeft(player, card, now) > TimeSpan.Zero)
        {
            return AkashicRefusal.Cooldown;
        }

        Put(player, new AkashicCallComponent(card.Id, yaw));
        StartCooldown(player, card, now);

        return AkashicRefusal.None;
    }

    /// <summary>
    /// The card of a quick slot when it is one the player has put into its book; nothing for an empty slot, one out of range or
    /// a card that is not in the book.
    /// </summary>
    private int? CardOf(Entity player, int slot)
    {
        if (slot is < 0 or >= Slots || !world.TryGet<InventoryComponent>(player, out var inventory) || inventory.Value is not { } bag)
        {
            return null;
        }

        var slots = bag.QuickSlotCards;

        if (slot >= slots.Length || slots[slot] == 0 || !_cards.TryGet(slots[slot], out var row) || bag.AkashicRecords.At(row.Position) is null)
        {
            return null;
        }

        return slots[slot];
    }

    /// <summary>
    /// How long until a card can be called again: the cooldown of its group, or the wait of all cards.
    /// </summary>
    public TimeSpan CooldownLeft(Entity player, in AkashicRecordsRow card, TimeSpan now)
    {
        if (!world.TryGet<AkashicCooldownComponent>(player, out var cooldowns))
        {
            return TimeSpan.Zero;
        }

        var left = cooldowns.Groups.TryGetValue(card.CooldownGroup, out var end) && end > now ? end - now : TimeSpan.Zero;

        return cooldowns.Global > now && cooldowns.Global - now > left ? cooldowns.Global - now : left;
    }

    private void StartCooldown(Entity player, in AkashicRecordsRow card, TimeSpan now)
    {
        var cooldowns = world.TryGet<AkashicCooldownComponent>(player, out var running) ? running : new AkashicCooldownComponent([], TimeSpan.Zero);
        var end = now + TimeSpan.FromMilliseconds(Math.Max(0, card.Cooldown));

        if (!cooldowns.Groups.TryGetValue(card.CooldownGroup, out var current) || end > current)
        {
            cooldowns.Groups[card.CooldownGroup] = end;
        }

        var global = now + TimeSpan.FromMilliseconds(Math.Max((short)0, card.CooldownGlobal));

        Put(player, cooldowns with { Global = global > cooldowns.Global ? global : cooldowns.Global });
    }

    /// <summary>
    /// Whether the skill is the akashic skill of a hero, the one that brings a called card in.
    /// </summary>
    public bool IsAkashicSkill(int skill) => _skills.TryGet(skill, out var row) && row.SkillType == AkashicSkillType;

    /// <summary>
    /// The card the player has called, for its akashic skill to bring in. The call is used up: the next skill needs a new one.
    /// </summary>
    public bool TryTake(Entity player, out AkashicRecordsRow card, out float yaw)
    {
        card = default;
        yaw = 0f;

        if (!world.IsAlive(player) || !world.TryGet<AkashicCallComponent>(player, out var call))
        {
            return false;
        }

        world.Remove<AkashicCallComponent>(player);
        yaw = call.Yaw;

        return _cards.TryGet(call.Card, out card) && Summons(card);
    }

    /// <summary>
    /// How long after the start of the akashic skill of a hero the card comes in, in seconds: the hero's own time, from the table
    /// of the heroes.
    /// </summary>
    public float SummonDelay(Entity player)
    {
        var hero = world.TryGet<PersonInfoComponent>(player, out var person) ? person.Hero : Hero.Haru;

        return _heroes.TryGet((short)((int)hero * 1000), out var row) ? Math.Max(0f, row.Akashic_Active_Time) : 0f;
    }

    private void Put<T>(Entity player, in T component)
    {
        if (world.Has<T>(player))
        {
            world.Set(player, component);
        }
        else
        {
            world.Add(player, component);
        }
    }
}
