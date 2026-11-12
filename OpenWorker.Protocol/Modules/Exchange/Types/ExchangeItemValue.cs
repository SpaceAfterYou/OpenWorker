using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Exchange.Types;

/// <summary>
/// A listing on sale as the search shows it to a buyer.
/// </summary>
[WireMessage]
public readonly partial struct ExchangeItemValue : IWritableData
{
    public required int Exchange { get; init; }
    public required long PriceOne { get; init; }

    /// <summary>
    /// When the sale ends, in Unix seconds.
    /// </summary>
    public required long ExpiresAt { get; init; }

    public required byte NoCommission { get; init; }
    public required ExchangeGoodsValue Goods { get; init; }
}
