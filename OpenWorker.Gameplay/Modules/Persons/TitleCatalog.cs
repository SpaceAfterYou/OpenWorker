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
/// <param name="Id">The number of the title.</param>
/// <param name="Kind">Before the name or after it.</param>
/// <param name="Class">The hero it belongs to, 0 for every hero.</param>
/// <param name="Effects">What it changes.</param>
/// <param name="OpenCondition">What opens it in the list (<see cref="OpenTitleManager.ByAchievement"/>,
/// <see cref="OpenTitleManager.ByLevel"/>, or another the server does not raise).</param>
/// <param name="OpenValue">The achievement or the level that opens it.</param>
public sealed record TitleDefinition(int Id, TitleKind Kind, byte Class, StatEffect[] Effects, byte OpenCondition = 0, int OpenValue = 0)
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
        Opening = Titles.Values.ToLookup(e => (e.OpenCondition, e.OpenValue));
    }

    private ILookup<(byte Condition, int Value), TitleDefinition> Opening { get; }

    private Dictionary<int, TitleDefinition> Titles { get; }

    public IEnumerable<TitleDefinition> All => Titles.Values;

    public bool TryGet(int id, out TitleDefinition title)
    {
        return Titles.TryGetValue(id, out title!);
    }

    /// <summary>
    /// The titles that open when a person of the hero does what the condition names with the value: the first prefix and the
    /// first suffix whose rows ask for exactly that and that the hero can use.
    /// </summary>
    public IEnumerable<int> OpenedBy(byte condition, int value, Hero hero)
    {
        var found = Opening[(condition, value)].Where(e => e.CanBeUsedBy(hero)).OrderBy(e => e.Id).ToArray();

        return new[] { TitleKind.Prefix, TitleKind.Suffix }
            .Select(kind => found.FirstOrDefault(e => e.Kind == kind))
            .OfType<TitleDefinition>()
            .Select(e => e.Id);
    }

    private static TitleDefinition Create(TitleInfoRow row)
    {
        // The table keeps each of the five effects in three columns.
        byte[] groups = [row.EffectGroup1, row.EffectGroup2, row.EffectGroup3, row.EffectGroup4, row.EffectGroup5];
        short[] types = [row.EffectType1, row.EffectType2, row.EffectType3, row.EffectType4, row.EffectType5];
        float[] values = [row.EffectValue1, row.EffectValue2, row.EffectValue3, row.EffectValue4, row.EffectValue5];

        var effects = new List<StatEffect>(EffectSlots);

        for (var slot = 0; slot < EffectSlots; slot++)
        {
            if (types[slot] != 0 && values[slot] != 0 && groups[slot] == CharacteristicEffect)
            {
                effects.Add(new StatEffect(types[slot], values[slot]));
            }
        }

        return new TitleDefinition(row.Id, (TitleKind)row.Type, row.Class, [.. effects], row.OpenCondition, row.OpenValue);
    }
}
