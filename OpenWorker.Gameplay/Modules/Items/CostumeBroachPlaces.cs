using System.Collections.ObjectModel;
using OpenWorker.Res;
using OpenWorker.Res.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// The places for broaches a piece of clothing comes with. It has five groups of three places: the first of a group takes an
/// attack broach, the second a defence one, the third a utility one. A place is open, closed until an item opens it, or
/// not there at all; the row of the clothing in <c>tb_Costume_Socket</c> (through the socket column of <c>tb_Item</c>)
/// gives how many of each a group has, the open ones first.
/// </summary>
public sealed class CostumeBroachPlaces(ItemCatalog catalog, ReadOnlyCollection<CostumeSocketRow> sockets)
{
    public const int Groups = 5;
    public const int PlacesPerGroup = 3;
    public const int Places = Groups * PlacesPerGroup;

    /// <summary>A place the clothing does not have.</summary>
    public const char None = '0';

    /// <summary>A place a broach may be set into.</summary>
    public const char Open = '1';

    /// <summary>A place an item has to open first.</summary>
    public const char Closed = '2';

    private readonly RowIndex<int, CostumeSocketRow> _sockets = sockets.IndexBy(e => e.Id);

    /// <summary>
    /// The row of a piece of clothing, or <c>null</c> for an item that is not clothing with places for broaches.
    /// </summary>
    public CostumeSocketRow? RowOf(int item)
    {
        return catalog.Classify(item)?.ItemUseType == ItemUseType.SHAPE && catalog.TryGet(item, out var row) && row.Socket != 0 &&
               _sockets.TryGet(row.Socket, out var socket)
            ? socket
            : null;
    }

    /// <summary>
    /// The places of a new item, one character each, as the client reads them; every place is missing for an item that is not
    /// clothing.
    /// </summary>
    public string StateOf(int item)
    {
        var state = new string(None, Places).ToCharArray();

        if (RowOf(item) is not { } row)
        {
            return new string(state);
        }

        byte[] open = [row.OpenPlaces1, row.OpenPlaces2, row.OpenPlaces3, row.OpenPlaces4, row.OpenPlaces5];
        byte[] closed = [row.ClosedPlaces1, row.ClosedPlaces2, row.ClosedPlaces3, row.ClosedPlaces4, row.ClosedPlaces5];

        for (var group = 0; group < Groups; group++)
        {
            // A group never has more than three places; a row that says otherwise ends the groups there.
            if (open[group] + closed[group] > PlacesPerGroup)
            {
                break;
            }

            for (var place = 0; place < open[group] + closed[group]; place++)
            {
                state[group * PlacesPerGroup + place] = place < open[group] ? Open : Closed;
            }
        }

        return new string(state);
    }
}
