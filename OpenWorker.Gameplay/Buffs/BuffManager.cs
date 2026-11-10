using System.Collections.ObjectModel;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Buffs;

/// <summary>
/// What every maze of the server shares about buffs: the buff table, the auras that give them to those around, and the passive
/// skills and cards that give them by themselves.
/// </summary>
public sealed class BuffManager
{
    private readonly RowIndex<short, AuraRow> _auras;

    public BuffManager(
        ReadOnlyCollection<BuffRow> buffs,
        ReadOnlyCollection<SkillRow>? skills = null,
        ReadOnlyCollection<DivergenceRow>? divergences = null,
        ReadOnlyCollection<AkashicRecordsRow>? cards = null,
        ReadOnlyCollection<AuraRow>? auras = null,
        ReadOnlyCollection<ComboBuffRow>? combos = null,
        ReadOnlyCollection<BuffSystemRow>? server = null)
    {
        Catalog = new BuffCatalog(buffs, server);
        Passives = new PassiveBuffs(skills ?? new ReadOnlyCollection<SkillRow>([]), divergences ?? new ReadOnlyCollection<DivergenceRow>([]), cards ?? new ReadOnlyCollection<AkashicRecordsRow>([]));
        _auras = (auras ?? new ReadOnlyCollection<AuraRow>([])).IndexBy(e => e.Id);
        Combos = (combos ?? new ReadOnlyCollection<ComboBuffRow>([])).OrderBy(e => e.Id).ToArray();
    }

    public BuffCatalog Catalog { get; }

    public PassiveBuffs Passives { get; }

    /// <summary>
    /// What the combos of a player give, by the number of hits the combo has to reach; the last one repeats.
    /// </summary>
    public IReadOnlyList<ComboBuffRow> Combos { get; }

    /// <summary>
    /// The aura of a number, when there is such an aura.
    /// </summary>
    public bool TryGetAura(int aura, out AuraRow row)
    {
        if (aura is > 0 and <= short.MaxValue)
        {
            return _auras.TryGet((short)aura, out row);
        }

        row = default;

        return false;
    }
}
