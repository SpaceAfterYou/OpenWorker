using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Shop.Types;

/// <summary>
/// A price of a cash item: how many come for it, the price before and after the discount.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct CashInfoValue
{
    public required int Item { get; init; }
    public required short Count { get; init; }
    public required int BasePrice { get; init; }
    public required int Price { get; init; }
}
