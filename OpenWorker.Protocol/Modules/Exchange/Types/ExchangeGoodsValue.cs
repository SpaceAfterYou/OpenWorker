using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Exchange.Types;

/// <summary>
/// The item of a listing as the client reads it after the fields of the listing: the item, its gems and its broaches.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct ExchangeGoodsValue
{
    public required ItemValue Item { get; init; }

    [WireArray(WireLength.Byte)]
    public required ItemSocketValue[] Sockets { get; init; }

    public required ItemBroachValue Broach { get; init; }
}
