using System.Collections.ObjectModel;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Buffs;

/// <summary>
/// The buffs a person has without being hit by anything: the lasting buffs of its passive skills and of the divergences it chose,
/// and the passive cards it keeps in its quick slots (which give a buff when something happens, or every few seconds while the
/// card is in its slot).
/// </summary>
public sealed class PassiveBuffs
{
    /// <summary>The skill type of a skill that works by itself.</summary>
    private const byte PassiveSkillType = 1;

    /// <summary>The passive type of a skill that raises the stats of its owner: its value is a buff.</summary>
    private const byte StatusPassiveType = 13;

    private readonly RowIndex<int, SkillRow> _skills;
    private readonly RowIndex<int, DivergenceRow> _divergences;
    private readonly RowIndex<int, AkashicRecordsRow> _cards;

    public PassiveBuffs(ReadOnlyCollection<SkillRow> skills, ReadOnlyCollection<DivergenceRow> divergences, ReadOnlyCollection<AkashicRecordsRow> cards)
    {
        _skills = skills.IndexBy(e => e.Id);
        _divergences = divergences.IndexBy(e => e.Id);
        _cards = cards.IndexBy(e => e.Id);
    }

    /// <summary>
    /// The card of a number, when there is such a card.
    /// </summary>
    public bool TryGetCard(int card, out AkashicRecordsRow row) => _cards.TryGet(card, out row);

    /// <summary>
    /// The buffs the learned skills of a person give: a passive skill of the status kind gives the buff its value names, and
    /// so does the divergence chosen for it when that is of the passive kind.
    /// </summary>
    public void Of(ReadOnlySpan<SkillInfoValue> learned, List<ushort> buffs)
    {
        foreach (var entry in learned)
        {
            if (entry.Skill == 0 || !_skills.TryGet(entry.Skill, out var skill) || skill.SkillType != PassiveSkillType || skill.PassiveType != StatusPassiveType)
            {
                continue;
            }

            Add(buffs, (ushort)skill.PassiveValue);

            if (entry.Divergence != 0 && _divergences.TryGet(entry.Divergence, out var divergence))
            {
                // A divergence that has another to stand for it gives what that one gives.
                if (divergence.SwapIdentifier != 0 && _divergences.TryGet(divergence.SwapIdentifier, out var swapped))
                {
                    divergence = swapped;
                }

                if (divergence.OptionType == DivergenceVariant.Passive)
                {
                    Add(buffs, (ushort)divergence.OptionValue);
                }
            }
        }
    }

    /// <summary>
    /// The cards in the quick slots of a person that do something by themselves: the ones whose card names a condition.
    /// </summary>
    public void CardsOf(ReadOnlySpan<int> slots, List<AkashicRecordsRow> cards)
    {
        foreach (var slot in slots)
        {
            if (slot != 0 && _cards.TryGet(slot, out var row) && row.InvokeCondition != 0 && row.Buff != 0)
            {
                cards.Add(row);
            }
        }
    }

    private static void Add(List<ushort> buffs, ushort buff)
    {
        if (buff != 0 && !buffs.Contains(buff))
        {
            buffs.Add(buff);
        }
    }
}
