using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Hotspot.Modules.Skill.Types;

namespace OpenWorker.Gameplay.Buffs;

/// <summary>
/// The buffs that the things a person has give without anything hitting it: the buffs of its passive skills and of the broach
/// sets in its clothing. These are given again whenever the person comes, so they are not carried from one server to the next.
/// </summary>
public sealed class SourceBuffs
{
    /// <summary>The buffs the sources have given and keep on the person.</summary>
    public List<ushort> Given { get; } = [];

    public List<ushort> Wanted { get; } = [];

    public List<ushort> Shown { get; } = [];
}

public static class BuffSources
{
    /// <summary>
    /// Makes the buffs of the sources what the person has: the buffs of the passive skills and of the clothing it wears are on it,
    /// the ones that came from something it no longer has are taken away. A buff a death took is given again.
    /// </summary>
    public static void Reconcile(
        BuffBook book,
        BuffCatalog catalog,
        PassiveBuffs passives,
        SkillInfoValue[]? learned,
        Inventory? inventory,
        SourceBuffs state,
        in BuffCarrier carrier,
        List<BuffChange> changes)
    {
        state.Wanted.Clear();
        state.Shown.Clear();

        if (learned is not null)
        {
            passives.Of(learned, state.Wanted);
        }

        passives.Clothing(inventory, state.Shown);

        for (var index = state.Given.Count - 1; index >= 0; index--)
        {
            var buff = state.Given[index];

            if (state.Wanted.Contains(buff) || state.Shown.Contains(buff))
            {
                continue;
            }

            var at = book.IndexOf(buff);

            if (at >= 0)
            {
                BuffRules.Remove(book, catalog, at, false, carrier, changes);
            }

            state.Given.RemoveAt(index);
        }

        foreach (var buff in state.Wanted)
        {
            if ((book.IndexOf(buff) >= 0 || (catalog.TryGet(buff, out var definition) && BuffRules.Apply(book, catalog, definition, carrier.Actor, carrier, changes))) && !state.Given.Contains(buff))
            {
                state.Given.Add(buff);
            }
        }

        foreach (var buff in state.Shown)
        {
            if (book.IndexOf(buff) >= 0)
            {
                BuffRules.MarkShown(book, buff, changes);
            }
            else if (!BuffRules.Show(book, catalog, buff, carrier, changes))
            {
                continue;
            }

            if (!state.Given.Contains(buff))
            {
                state.Given.Add(buff);
            }
        }
    }
}
