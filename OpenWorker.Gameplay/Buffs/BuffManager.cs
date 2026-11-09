using System.Collections.ObjectModel;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Buffs;

/// <summary>
/// What every maze of the server shares about buffs: the buff table.
/// </summary>
public sealed class BuffManager(ReadOnlyCollection<BuffRow> buffs)
{
    public BuffCatalog Catalog { get; } = new(buffs);
}
