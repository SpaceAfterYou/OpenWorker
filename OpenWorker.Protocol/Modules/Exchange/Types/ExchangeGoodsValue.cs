using OpenWorker.Hotspot.Modules.Items.Extensions;
using OpenWorker.Hotspot.Modules.Items.Types;

namespace OpenWorker.Hotspot.Modules.Exchange.Types;

/// <summary>
/// The item of a listing as the client reads it after the fields of the listing: the item, its gems and its broaches.
/// </summary>
public readonly struct ExchangeGoodsValue
{
    public required ItemValue Item { get; init; }
    public required ItemSocketValue[] Sockets { get; init; }
    public required ItemBroachValue Broach { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Item);
        writer.Write(Sockets);
        Broach.Write(writer);
    }
}
