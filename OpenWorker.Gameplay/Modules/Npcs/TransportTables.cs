using System.Collections.ObjectModel;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Npcs;

/// <summary>
/// The rides of the NPCs of transports: <c>tb_District_Transport</c> says which rides an NPC offers (its transport
/// function names the row, the row lists up to five), <c>tb_Transport_Info</c> says what a ride costs, which path it follows,
/// for how many seconds, and which box of the map it ends at.
/// </summary>
public sealed class TransportTables(ReadOnlyCollection<DistrictTransportRow> offers, ReadOnlyCollection<TransportInfoRow> rides)
{
    private readonly RowIndex<short, DistrictTransportRow> _offers = offers.IndexBy(e => e.Id);
    private readonly RowIndex<short, TransportInfoRow> _rides = rides.IndexBy(e => e.Id);

    /// <summary>
    /// The ride a player asks an NPC for, when the NPC offers it: <paramref name="function"/> is the row of the NPC in the
    /// table of offers, <paramref name="row"/> the row of the ride the client names.
    /// </summary>
    public bool TryGetRide(int function, short row, out TransportInfoRow ride)
    {
        ride = default;

        if (row == 0 || function is <= 0 or > short.MaxValue || !_offers.TryGet((short)function, out var offer))
        {
            return false;
        }

        return (offer.Transport1 == row || offer.Transport2 == row || offer.Transport3 == row || offer.Transport4 == row ||
                offer.Transport5 == row) && _rides.TryGet(row, out ride);
    }
}
