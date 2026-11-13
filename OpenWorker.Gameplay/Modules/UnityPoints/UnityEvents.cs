using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.UnityPoints;

/// <summary>
/// The unity events a maze script can start (<c>tb_Unity_Event</c>), and the purse the battle points of their combos are paid
/// into.
/// </summary>
public sealed class UnityEvents(ReadOnlyCollection<UnityEventRow> events, InventoryService? inventory = null)
{
    private readonly RowIndex<ushort, UnityEventRow> _events = events.IndexBy(e => e.Id);

    /// <summary>
    /// Whether the table has the event of that number.
    /// </summary>
    public bool Has(int table)
    {
        return table is > 0 and <= ushort.MaxValue && _events.TryGet((ushort)table, out _);
    }

    /// <summary>
    /// Pays the battle points of the tally into the purse of the player, up to the limit of the purse. The player is told its
    /// purse by the inventory.
    /// </summary>
    public void Pay(Entity player, UnityTally tally)
    {
        var points = tally.TakeBattlePoints();

        if (points > 0)
        {
            inventory?.AddUpToLimit(player, ShopCurrency.Bp, points);
        }
    }
}
