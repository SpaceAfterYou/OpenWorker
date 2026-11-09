using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Skill;

/// <summary>
/// What a person may learn and how its points grow: the rules of the skill window, checked on the server in the order of
/// the original server. Every refusal is the code <see cref="SkillRefusal.CannotLearn"/> and changes nothing.
/// </summary>
public sealed class SkillLearning(
    World world,
    ReadOnlyCollection<SkillRow> skills,
    ReadOnlyCollection<DivergenceRow> divergences,
    ReadOnlyCollection<LevelSkillPointRow> levels)
{
    /// <summary>
    /// The most skills a position of a deck holds, and the slots a deck starts with.
    /// </summary>
    public const short MaxDeckSlots = 4;

    public const int DeckPositions = 6;

    private readonly RowIndex<int, SkillRow> _skills = skills.IndexBy(e => e.Id);
    private readonly RowIndex<int, DivergenceRow> _divergences = divergences.IndexBy(e => e.Id);
    private readonly short[] _pointsByLevel = Accumulate(levels);

    /// <summary>
    /// Whether the server's own table of points per level is there: without it the points do not grow with the level.
    /// </summary>
    public bool KnowsPoints => _pointsByLevel.Length > 0;

    /// <summary>
    /// All the points a character has earned by reaching a level: what every level after the first gave.
    /// </summary>
    public short PointsAt(int level)
    {
        return _pointsByLevel.Length == 0 ? (short)0 : _pointsByLevel[Math.Clamp(level, 0, _pointsByLevel.Length - 1)];
    }

    /// <summary>
    /// Learns a skill, or the next level of a skill the person has: replaces the earlier level in the book and in the decks.
    /// Returns the refusal, or <see cref="SkillRefusal.None"/> and the skill it replaced (0 for a new one).
    /// </summary>
    public (ushort Refusal, int Replaced) Learn(Entity player, int skill)
    {
        if (!_skills.TryGet(skill, out var row) ||
            !world.TryGet<SkillLearnedComponent>(player, out var learned) || learned.Values is null ||
            !world.TryGet<SkillPointComponent>(player, out var points))
        {
            return (SkillRefusal.CannotLearn, 0);
        }

        var values = learned.Values;
        var index = IndexOfGroup(values, row.Group);

        // The next level of a skill, one after the other; a new skill from its first level.
        if (index >= 0)
        {
            if (!_skills.TryGet(values[index].Skill, out var own) || row.Level - own.Level != 1)
            {
                return (SkillRefusal.CannotLearn, 0);
            }
        }
        else if (row.Level != 1)
        {
            return (SkillRefusal.CannotLearn, 0);
        }

        var level = world.TryGet<LevelComponent>(player, out var reached) ? reached.Level : (byte)1;

        if (row.RequiredLevel > level ||
            (row.UseClass != 0 && world.TryGet<PersonInfoComponent>(player, out var person) && row.UseClass != (byte)person.Hero) ||
            points.FreeSkillPoint < row.RequiredPoints ||
            points.TotalSkillPoint - points.FreeSkillPoint < row.RequiredSpentPoints ||
            (row.RequiredSkill != 0 && Array.FindIndex(values, e => e.Skill == row.RequiredSkill) < 0))
        {
            return (SkillRefusal.CannotLearn, 0);
        }

        var replaced = index >= 0 ? values[index].Skill : 0;
        var book = values.Where(e => e.Skill != 0).ToList();

        if (index >= 0)
        {
            book[book.FindIndex(e => e.Skill == replaced)] = values[index] with { Skill = skill };
        }
        else
        {
            book.Add(new SkillInfoValue { Skill = skill });
        }

        world.Set(player, learned with { Values = [.. book] });
        world.Set(player, points with { FreeSkillPoint = (short)(points.FreeSkillPoint - row.RequiredPoints) });

        if (replaced != 0)
        {
            ReplaceInDecks(player, replaced, skill);
        }

        world.MarkDirty(player, PersonSections.Skills);

        return (SkillRefusal.None, replaced);
    }

    /// <summary>
    /// Chooses the divergence of a learned skill, or its next level: the divergence belongs to a group of the skill, the
    /// level and the points are enough, and a level follows the one the skill has.
    /// </summary>
    public ushort LearnDivergence(Entity player, int skill, int divergence)
    {
        if (!_skills.TryGet(skill, out var row) || !_divergences.TryGet(divergence, out var next) ||
            !world.TryGet<SkillLearnedComponent>(player, out var learned) || learned.Values is null ||
            !world.TryGet<SkillPointComponent>(player, out var points))
        {
            return SkillRefusal.CannotLearn;
        }

        var index = Array.FindIndex(learned.Values, e => e.Skill == skill);
        var level = world.TryGet<LevelComponent>(player, out var reached) ? reached.Level : (byte)1;

        if (index < 0 || (next.Group != row.DivergenceGroup1 && next.Group != row.DivergenceGroup2) ||
            next.NeedLevel > level || next.NeedPointCount > points.FreeSkillPoint)
        {
            return SkillRefusal.CannotLearn;
        }

        // A divergence of the same group goes one level at a time; another group starts again from its first level.
        var current = learned.Values[index].Divergence;

        if (current != 0 && _divergences.TryGet(current, out var had) && had.Group == next.Group)
        {
            if (had.Next != next.Id)
            {
                return SkillRefusal.CannotLearn;
            }
        }
        else if (next.Level != 1)
        {
            return SkillRefusal.CannotLearn;
        }

        var values = (SkillInfoValue[])learned.Values.Clone();
        values[index] = values[index] with { Divergence = divergence };

        world.Set(player, learned with { Values = values });
        world.Set(player, points with { FreeSkillPoint = (short)(points.FreeSkillPoint - next.NeedPointCount) });
        world.MarkDirty(player, PersonSections.Skills);

        return SkillRefusal.None;
    }

    /// <summary>
    /// Puts skills into the deck. Every skill must be learned and every position must be one of the deck; otherwise nothing
    /// changes.
    /// </summary>
    public bool UpdateDeck(Entity player, IReadOnlyList<SkillDeckValue> slots)
    {
        if (!world.TryGet<SkillDeckComponent>(player, out var deck) ||
            !world.TryGet<SkillLearnedComponent>(player, out var learned) || learned.Values is null)
        {
            return false;
        }

        var size = Math.Clamp((int)deck.SlotCount, 1, MaxDeckSlots);

        foreach (var slot in slots)
        {
            if (slot.Position >= DeckPositions)
            {
                return false;
            }

            var index = 0;

            foreach (var skill in slot.GetList())
            {
                if (skill != 0 && (index >= size || Array.FindIndex(learned.Values, e => e.Skill == skill) < 0))
                {
                    return false;
                }

                index++;
            }
        }

        var decks = (deck.SlotList ?? []).ToList();

        foreach (var slot in slots)
        {
            var at = decks.FindIndex(e => e.Position == slot.Position);

            if (at >= 0)
            {
                decks[at] = slot;
            }
            else
            {
                decks.Add(slot);
            }
        }

        world.Set(player, deck with { SlotList = [.. decks] });
        world.MarkDirty(player, PersonSections.Skills);

        return true;
    }

    /// <summary>
    /// Gives a deck one more slot, up to <see cref="MaxDeckSlots"/>. Returns the new count, or 0 when it is full.
    /// </summary>
    public short AddDeckSlot(Entity player)
    {
        if (!world.TryGet<SkillDeckComponent>(player, out var deck) || deck.SlotCount >= MaxDeckSlots)
        {
            return 0;
        }

        var count = (short)(deck.SlotCount + 1);

        world.Set(player, deck with { SlotCount = count });
        world.MarkDirty(player, PersonSections.Skills);

        return count;
    }

    /// <summary>
    /// Forgets every skill: the person keeps the default skills of its hero, every point comes back and the decks empty.
    /// </summary>
    public void Reset(Entity player, SkillInfoValue[] defaults)
    {
        if (!world.TryGet<SkillPointComponent>(player, out var points))
        {
            return;
        }

        world.Set(player, new SkillLearnedComponent { Values = defaults });
        world.Set(player, points with { FreeSkillPoint = points.TotalSkillPoint });

        if (world.TryGet<SkillDeckComponent>(player, out var deck))
        {
            world.Set(player, deck with { SlotList = [] });
        }

        world.MarkDirty(player, PersonSections.Skills);
    }

    /// <summary>
    /// A person that reached a level gets what the levels it passed give, and is told. Nothing happens to a person whose
    /// skills are not loaded on this server: the next server that loads them counts the points of its level.
    /// </summary>
    public void OnLevelReached(Entity player, int from, int to)
    {
        if (!KnowsPoints || to <= from || !world.TryGet<SkillPointComponent>(player, out var points))
        {
            return;
        }

        var gained = (short)(PointsAt(to) - PointsAt(from));

        if (gained <= 0)
        {
            return;
        }

        Grow(player, points, gained);
    }

    /// <summary>
    /// A person loaded with fewer points than its level has earned (it rose on a server that did not hold its skills, or
    /// before the points followed the level) gets the difference.
    /// </summary>
    public void CatchUp(Entity player)
    {
        if (!KnowsPoints || !world.TryGet<SkillPointComponent>(player, out var points))
        {
            return;
        }

        var level = world.TryGet<LevelComponent>(player, out var reached) ? reached.Level : (byte)1;
        var owed = (short)(PointsAt(level) - points.TotalSkillPoint);

        if (owed > 0)
        {
            Grow(player, points, owed);
        }
    }

    private void Grow(Entity player, SkillPointComponent points, short gained)
    {
        var grown = new SkillPointComponent
        {
            TotalSkillPoint = (short)(points.TotalSkillPoint + gained),
            FreeSkillPoint = (short)(points.FreeSkillPoint + gained)
        };

        world.Set(player, grown);
        world.MarkDirty(player, PersonSections.Skills);

        if (world.TryGet<ServerSessionComponent>(player, out var session))
        {
            session.Send(new SkillUpdatePointResponse
            {
                SkillPoint = new SkillPointValue { TotalSkillPoint = grown.TotalSkillPoint, FreeSkillPoint = grown.FreeSkillPoint }
            });
        }
    }

    private void ReplaceInDecks(Entity player, int old, int skill)
    {
        if (!world.TryGet<SkillDeckComponent>(player, out var deck) || deck.SlotList is null)
        {
            return;
        }

        var slots = deck.SlotList.Select(e => e with
        {
            Skill1 = e.Skill1 == old ? skill : e.Skill1,
            Skill2 = e.Skill2 == old ? skill : e.Skill2,
            Skill3 = e.Skill3 == old ? skill : e.Skill3,
            Skill4 = e.Skill4 == old ? skill : e.Skill4
        }).ToArray();

        world.Set(player, deck with { SlotList = slots });
    }

    private int IndexOfGroup(SkillInfoValue[] values, int group)
    {
        for (var i = 0; i < values.Length; i++)
        {
            if (values[i].Skill != 0 && _skills.TryGet(values[i].Skill, out var own) && own.Group == group)
            {
                return i;
            }
        }

        return -1;
    }

    private static short[] Accumulate(ReadOnlyCollection<LevelSkillPointRow> levels)
    {
        if (levels.Count == 0)
        {
            return [];
        }

        var top = levels.Max(e => e.Level);
        var sums = new short[top + 1];
        var given = levels.GroupBy(e => e.Level).ToDictionary(e => (int)e.Key, e => e.First().SkillPoints);

        // A character starts at the first level with nothing: each level after it gives its points when it is reached.
        for (var level = 2; level <= top; level++)
        {
            sums[level] = (short)(sums[level - 1] + given.GetValueOrDefault(level));
        }

        sums[0] = 0;

        return sums;
    }
}
