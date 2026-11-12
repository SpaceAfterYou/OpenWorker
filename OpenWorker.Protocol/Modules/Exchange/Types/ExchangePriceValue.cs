using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Exchange.Types;

/// <summary>
/// One sale of an item: how many went, at what price for one, and when (Unix seconds).
/// </summary>
[WireMessage]
public readonly partial struct ExchangePriceValue
{
    public int Item { get; init; }
    public short Count { get; init; }
    public long PriceOne { get; init; }
    public long SoldAt { get; init; }
}
