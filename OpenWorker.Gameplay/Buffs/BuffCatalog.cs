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

    /// <param name="server">The server's own table, which says the system type of a buff; the client's does not.</param>
    public BuffCatalog(ReadOnlyCollection<BuffRow> rows, ReadOnlyCollection<BuffSystemRow>? server = null) : this(Definitions(rows, server))
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

    /// <summary>
    /// The buffs of the client's table, each with the system type the server's table gives it, when that table has the same buff
    /// (the same kind, effect and group) under the number.
    /// </summary>
    private static IEnumerable<BuffDefinition> Definitions(ReadOnlyCollection<BuffRow> rows, ReadOnlyCollection<BuffSystemRow>? server)
    {
        var systems = (server ?? new ReadOnlyCollection<BuffSystemRow>([])).IndexBy(e => e.Id);

        foreach (var row in rows)
        {
            var definition = BuffDefinition.From(row);

            if (systems.TryGet(definition.Id, out var system) && system.Kind == row.Kind && system.Effect == row.Effect && system.Group == row.Group)
            {
                definition = definition with { SystemType = system.SystemType };
            }

            yield return definition;
        }
    }
}
