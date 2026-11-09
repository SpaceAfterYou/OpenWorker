using System.Collections.ObjectModel;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Buffs;

/// <summary>
/// The buffs of the buff table, by their number.
/// </summary>
public sealed class BuffCatalog
{
    private readonly RowIndex<ushort, BuffDefinition> _buffs;

    public BuffCatalog(ReadOnlyCollection<BuffRow> rows) : this(rows.Select(e => BuffDefinition.From(e)))
    {
    }

    public BuffCatalog(IEnumerable<BuffDefinition> buffs)
    {
        _buffs = buffs.IndexBy(e => e.Id);
    }

    /// <summary>
    /// Buffs are named by an unsigned 16-bit number; anything else is not a buff.
    /// </summary>
    public bool TryGet(int buff, out BuffDefinition definition)
    {
        if (buff is <= 0 or > ushort.MaxValue)
        {
            definition = default;

            return false;
        }

        return _buffs.TryGet((ushort)buff, out definition);
    }
}
