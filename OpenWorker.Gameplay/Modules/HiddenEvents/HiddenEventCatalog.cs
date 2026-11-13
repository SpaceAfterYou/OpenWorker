using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Gameplay.Modules.Boosters;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Res;
using OpenWorker.Res.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.HiddenEvents;

/// <summary>
/// The hidden events of the maps and how one starts. A box of a map whose event is hidden starts one the first time a player
/// steps in, by its chance out of ten thousand, or by the chance of a booster of hidden events when the player has one; the
/// event is the row of <c>tb_Hidden_Event</c> under the id of the box, and its condition is picked by the weights of the row.
/// The conditions and their counts are the client's, which its window shows; the reward is the server's when its table has the
/// row. A condition of a kind the server does not know is never picked: it could only fail, or only be met.
/// </summary>
public sealed class HiddenEventCatalog
{
    private const int Certain = 10000;

    // A point of the booster is a hundredth of the chance.
    private const int BoosterStep = 100;

    // The kinds of skill the table numbers that the conditions count.
    private const byte PeakSkillType = 4;
    private const byte ErosionSkillType = 5;
    private const byte JumpAttackPassive = 16;

    // The rows of a deck on the page the client uses, and the places of a row that link to the skill before them.
    private const int DeckRows = 6;
    private const int DeckPlaces = 4;

    private static readonly HashSet<HiddenEventTarget> Counted =
    [
        HiddenEventTarget.Monster,
        HiddenEventTarget.Jump,
        HiddenEventTarget.Stamina,
        HiddenEventTarget.Gold,
        HiddenEventTarget.Ether,
        HiddenEventTarget.Soul,
        HiddenEventTarget.MaxCombo,
        HiddenEventTarget.ErosionSkill,
        HiddenEventTarget.PeakSkill,
        HiddenEventTarget.ItemUsed,
        HiddenEventTarget.ArmorBreak,
        HiddenEventTarget.SkillLink,
        HiddenEventTarget.JumpAttack,
        HiddenEventTarget.Evade,
        HiddenEventTarget.KillInAir,
        HiddenEventTarget.JumpAttackKill
    ];

    private readonly RowIndex<short, HiddenEventRow> _events;
    private readonly RowIndex<int, EventConditionRow> _conditions;
    private readonly RowIndex<ushort, HiddenEventRewardRow> _rewards;
    private readonly RowIndex<short, DeckBonusRow> _deckBonuses;
    private readonly Dictionary<int, (byte Type, byte Passive, int Swap)> _skills;
    private readonly ItemCatalog? _items;
    private readonly BoosterManager? _boosters;
    private readonly Random _random;

    public HiddenEventCatalog(
        ReadOnlyCollection<HiddenEventRow> events,
        ReadOnlyCollection<EventConditionRow> conditions,
        ReadOnlyCollection<HiddenEventRewardRow> rewards,
        ReadOnlyCollection<SkillRow> skills,
        ReadOnlyCollection<DeckBonusRow>? deckBonuses = null,
        ItemCatalog? items = null,
        BoosterManager? boosters = null,
        Random? random = null)
    {
        _events = events.IndexBy(e => e.Id);
        _conditions = conditions.IndexBy(e => e.Id);
        _rewards = rewards.IndexBy(e => e.Id);
        _deckBonuses = (deckBonuses ?? new ReadOnlyCollection<DeckBonusRow>([])).IndexBy(e => e.Id);
        _skills = skills.DistinctBy(e => e.Id).ToDictionary(e => e.Id, e => (e.SkillType, e.PassiveType, e.SwapSkill));
        _items = items;
        _boosters = boosters;
        _random = random ?? Random.Shared;
    }

    /// <summary>
    /// The player stepped into the box of a hidden event whose chance is <paramref name="rate"/> out of ten thousand: the event
    /// that starts, or nothing when the roll fails or the box has no event.
    /// </summary>
    public HiddenEvent? TryStart(Entity player, int box, float rate)
    {
        var roll = _random.Next(1, Certain + 1);
        var booster = _boosters?.GetValue(player, BoosterEffect.HiddenEvent) ?? 0f;

        var starts = booster > 0f ? roll <= BoosterStep * booster : roll <= rate;

        return starts ? Pick(box) : null;
    }

    /// <summary>
    /// The event of the box with a condition picked by the weights of its row, or nothing when it has no condition the server
    /// counts.
    /// </summary>
    public HiddenEvent? Pick(int box)
    {
        if (box is <= 0 or > ushort.MaxValue || !_events.TryGet(unchecked((short)box), out var row))
        {
            return null;
        }

        var choices = new List<(HiddenEventCondition Condition, int Weight)>(5);

        Add(row.Condition1, row.Rate1);
        Add(row.Condition2, row.Rate2);
        Add(row.Condition3, row.Rate3);
        Add(row.Condition4, row.Rate4);
        Add(row.Condition5, row.Rate5);

        var total = choices.Sum(e => e.Weight);

        if (total <= 0)
        {
            return null;
        }

        var roll = _random.Next(1, total + 1);

        foreach (var (condition, weight) in choices)
        {
            roll -= weight;

            if (roll <= 0)
            {
                var reward = _rewards.TryGet((ushort)box, out var paid) ? new HiddenEventReward(paid.Item, paid.Count) : new HiddenEventReward(row.RewardItem, row.RewardCount);

                return new HiddenEvent((ushort)box, condition, reward, CountsAnyItem);
            }
        }

        return null;

        void Add(int id, short weight)
        {
            if (id != 0 && weight > 0 && _conditions.TryGet(id, out var found) && Counted.Contains((HiddenEventTarget)found.Target) &&
                Enum.IsDefined((HiddenEventKind)found.Kind))
            {
                choices.Add((new HiddenEventCondition(found.Id, (HiddenEventKind)found.Kind, (HiddenEventTarget)found.Target, found.TargetId, found.Count), weight));
            }
        }
    }

    /// <summary>
    /// What starting a skill counts: the skills of the kinds the table numbers 4 and 5, and the jump attacks.
    /// </summary>
    public IEnumerable<HiddenEventTarget> SkillTargets(int skill)
    {
        if (!_skills.TryGetValue(skill, out var kind))
        {
            yield break;
        }

        if (kind.Type == PeakSkillType)
        {
            yield return HiddenEventTarget.PeakSkill;
        }
        else if (kind.Type == ErosionSkillType)
        {
            yield return HiddenEventTarget.ErosionSkill;
        }

        if (kind.Passive == JumpAttackPassive)
        {
            yield return HiddenEventTarget.JumpAttack;
        }
    }

    /// <summary>
    /// What a blow of a player with the skill counts: the armor of the monster it broke, the monster it killed while the monster
    /// was in the air, the monster a jump attack killed.
    /// </summary>
    public IEnumerable<HiddenEventTarget> BlowTargets(int skill, bool brokeArmor, bool killed, bool aloft)
    {
        if (brokeArmor)
        {
            yield return HiddenEventTarget.ArmorBreak;
        }

        if (!killed || skill <= 0)
        {
            yield break;
        }

        if (_skills.TryGetValue(skill, out var kind) && kind.Passive == JumpAttackPassive)
        {
            yield return HiddenEventTarget.JumpAttackKill;
        }

        if (aloft)
        {
            yield return HiddenEventTarget.KillInAir;
        }
    }

    /// <summary>
    /// Whether the skill is used linked from the deck: it stands in the row of the deck the client names, after the first place,
    /// and the player has chosen a bonus of the table for that place.
    /// </summary>
    public bool IsDeckLink(SkillDeckValue[]? decks, ushort[]? bonuses, int skill, byte row)
    {
        if (row >= DeckRows || decks is null || bonuses is null || skill == 0)
        {
            return false;
        }

        var swap = _skills.TryGetValue(skill, out var kind) ? kind.Swap : 0;

        foreach (var deck in decks)
        {
            if (deck.Position != row)
            {
                continue;
            }

            var place = 0;

            foreach (var standing in deck.GetList())
            {
                if (standing != 0 && (standing == skill || standing == swap))
                {
                    return place is > 0 and < DeckPlaces && place - 1 < bonuses.Length && bonuses[place - 1] is var bonus and > 0 &&
                           bonus <= short.MaxValue && _deckBonuses.TryGet((short)bonus, out _);
                }

                place++;
            }
        }

        return false;
    }

    private bool CountsAnyItem(int item)
    {
        return _items?.Classify(item) is { ItemUseType: ItemUseType.EFFECT };
    }
}
