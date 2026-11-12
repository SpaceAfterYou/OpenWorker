using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Extensions;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Trade.Types;

/// <summary>
/// An item of a shop of a player and its price. It is read by the generator; it is still written by hand, because the
/// item has no span writer yet.
/// </summary>
[WireMessage(WireMethods.Read)]
public readonly partial struct PrivateShopItemValue : IWritableData
{
    public ItemValue Item { get; init; }
    public long Money { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Item);
        writer.Write(Money);
    }
}
