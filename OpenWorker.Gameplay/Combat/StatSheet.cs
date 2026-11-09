using OpenWorker.Gameplay.Modules.Persons;
using OpenWorker.Hotspot.Modules.Persons.Responses;

namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// What everything a character wears, owns and carries adds to its stats, before the formulas of its status turn them into the
/// stats it fights with: for every stat a number added and a share in percent. Gear, its options, gems, upgrades, sets,
/// broaches, titles and buffs all write here, through the map of stat effects where they name an effect.
/// </summary>
public sealed class StatSheet
{
    public const int Count = (int)STAT_TYPE.MAX_STAT;

    private readonly float[] _added = new float[Count];
    private readonly float[] _shares = new float[Count];

    /// <summary>
    /// Nothing has been written.
    /// </summary>
    public bool IsEmpty { get; private set; } = true;

    /// <summary>
    /// The number added to a stat.
    /// </summary>
    public float Added(STAT_TYPE stat) => _added[(int)stat];

    /// <summary>
    /// The share in percent a stat grows by.
    /// </summary>
    public float Share(STAT_TYPE stat) => _shares[(int)stat];

    public void Add(STAT_TYPE stat, float value)
    {
        if ((int)stat < Count && value != 0f)
        {
            _added[(int)stat] += value;
            IsEmpty = false;
        }
    }

    public void Grow(STAT_TYPE stat, float percent)
    {
        if ((int)stat < Count && percent != 0f)
        {
            _shares[(int)stat] += percent;
            IsEmpty = false;
        }
    }

    /// <summary>
    /// Writes a stat effect where the map of effects puts it. An effect that is not a stat (a special effect, a current value
    /// such as the life left) writes nothing and answers <c>false</c>.
    /// </summary>
    public bool Apply(int effect, float value)
    {
        if (!StatEffects.TryMap(effect, out var stat, out var added) || StatEffects.IsCurrentValue(stat))
        {
            return false;
        }

        if (added)
        {
            Add(stat, value);
        }
        else
        {
            Grow(stat, value);
        }

        return true;
    }

    public void Apply(ReadOnlySpan<StatEffect> effects)
    {
        foreach (var effect in effects)
        {
            Apply(effect.Type, effect.Value);
        }
    }

    /// <summary>
    /// The flat bonuses of a table (a monster, a helper) as numbers added to their stats.
    /// </summary>
    public static StatSheet From(in StatBonuses bonuses)
    {
        var sheet = new StatSheet();

        sheet.Add(STAT_TYPE.STAT_HP_MAX, bonuses.Health);
        sheet.Add(STAT_TYPE.STAT_PATK_MAX, bonuses.PhysicalAttack);
        sheet.Add(STAT_TYPE.STAT_MATK_MAX, bonuses.MagicalAttack);
        sheet.Add(STAT_TYPE.STAT_PDEF, bonuses.PhysicalDefense);
        sheet.Add(STAT_TYPE.STAT_MDEF, bonuses.MagicalDefense);
        sheet.Add(STAT_TYPE.STAT_PAR, bonuses.PhysicalAccuracy);
        sheet.Add(STAT_TYPE.STAT_MAR, bonuses.MagicalAccuracy);
        sheet.Add(STAT_TYPE.STAT_PARP, bonuses.PhysicalEvasion);
        sheet.Add(STAT_TYPE.STAT_MARP, bonuses.MagicalEvasion);
        sheet.Add(STAT_TYPE.STAT_ADR, bonuses.GrazeDamage);
        sheet.Add(STAT_TYPE.STAT_PCP, bonuses.PhysicalCriticalChance);
        sheet.Add(STAT_TYPE.STAT_MCP, bonuses.MagicalCriticalChance);
        sheet.Add(STAT_TYPE.STAT_PCRP, bonuses.PhysicalCriticalResist);
        sheet.Add(STAT_TYPE.STAT_MCRP, bonuses.MagicalCriticalResist);
        sheet.Add(STAT_TYPE.STAT_PCA, bonuses.PhysicalCriticalDamage);
        sheet.Add(STAT_TYPE.STAT_PDSR, bonuses.PhysicalDamageReduction);
        sheet.Add(STAT_TYPE.STAT_MDSR, bonuses.MagicalDamageReduction);

        return sheet;
    }
}
