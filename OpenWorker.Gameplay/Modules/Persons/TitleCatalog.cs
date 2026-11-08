using System.Collections.ObjectModel;
using OpenWorker.Domain.Enums;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Persons;

/// <summary>
/// A title goes before the name (a prefix) or after it (a suffix), and a person wears one of each.
/// </summary>
public enum TitleKind : byte
{
    Prefix = 0,
    Suffix = 1
}

/// <summary>
/// One characteristic a title changes: which one, and by how much.
/// </summary>
public readonly record struct StatEffect(int Type, float Value);

/// <summary>
/// A title as the table describes it.
/// </summary>
public sealed record TitleDefinition(int Id, TitleKind Kind, byte Class, StatEffect[] Effects)
{
    /// <summary>
    /// Titles of class 0 belong to every hero, the others to the hero of that number.
    /// </summary>
    public bool CanBeUsedBy(Hero hero)
    {
        return Class == 0 || Class == (byte)hero;
    }
}

/// <summary>
/// Every title there is.
/// </summary>
public sealed class TitleCatalog
{
    private const int EffectSlots = 5;

    /// <summary>
    /// The effects in the table work on the characteristics. Others (of a skill, say) have a different meaning for the same number.
    /// </summary>
    private const byte CharacteristicEffect = 0;

    public TitleCatalog(ReadOnlyCollection<TitleInfoRow> rows)
    {
        Titles = rows.DistinctBy(e => e.Id).ToDictionary(e => e.Id, Create);
    }

    private Dictionary<int, TitleDefinition> Titles { get; }

    public IEnumerable<TitleDefinition> All => Titles.Values;

    public bool TryGet(int id, out TitleDefinition title)
    {
        return Titles.TryGetValue(id, out title!);
    }

    private static TitleDefinition Create(TitleInfoRow row)
    {
        // The table keeps each of the five effects in three columns.
        byte[] groups = [row.Field8, row.Field9, row.Field10, row.Field11, row.Field12];
        short[] types = [row.Field13, row.Field14, row.Field15, row.Field16, row.Field17];
        float[] values = [row.Field18, row.Field19, row.Field20, row.Field21, row.Field22];

        var effects = new List<StatEffect>(EffectSlots);

        for (var slot = 0; slot < EffectSlots; slot++)
        {
            if (types[slot] != 0 && values[slot] != 0 && groups[slot] == CharacteristicEffect)
            {
                effects.Add(new StatEffect(types[slot], values[slot]));
            }
        }

        return new TitleDefinition(row.Id, (TitleKind)row.Type, row.Class, [.. effects]);
    }
}
