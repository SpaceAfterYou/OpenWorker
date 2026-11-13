using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Trade.Types;

/// <summary>
/// An item of a shop of a player and its price.
/// </summary>
[WireMessage(WireMethods.Both)]
public readonly partial struct PrivateShopItemValue
{
    public ItemValue Item { get; init; }
    public long Money { get; init; }
}
