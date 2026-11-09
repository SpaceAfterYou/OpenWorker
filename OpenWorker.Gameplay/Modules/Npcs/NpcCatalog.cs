using System.Collections.ObjectModel;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Npcs;

/// <summary>
/// The rows of the NPCs (<c>tb_NPC</c>): the group whose credit an NPC raises and the functions it offers.
/// </summary>
public sealed class NpcCatalog(ReadOnlyCollection<NPCRow> rows)
{
    private readonly RowIndex<int, NPCRow> _rows = rows.IndexBy(e => e.Id);

    public bool Exists(int npc) => _rows.TryGet(npc, out _);

    /// <summary>
    /// The group of NPCs the credit of this one belongs to, 0 for an NPC that keeps no credit.
    /// </summary>
    public int GroupOf(int npc) => _rows.TryGet(npc, out var row) ? row.Group : 0;

    /// <summary>
    /// The id of the first function of the NPC of that kind (a shop group, a recipe group, a transport row, or 0 for a
    /// function without one), or <c>null</c> when the NPC does not offer it.
    /// </summary>
    public int? FunctionOf(int npc, NpcFunction function)
    {
        if (!_rows.TryGet(npc, out var row))
        {
            return null;
        }

        if (row.FunctionType1 == (byte)function)
        {
            return row.Function1;
        }

        if (row.FunctionType2 == (byte)function)
        {
            return row.Function2;
        }

        return row.FunctionType3 == (byte)function ? row.Function3 : null;
    }
}
