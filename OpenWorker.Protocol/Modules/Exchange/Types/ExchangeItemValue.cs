using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Exchange.Types;

/// <summary>
/// A listing on sale as the search shows it to a buyer.
/// </summary>
public readonly struct ExchangeItemValue : IWritableData
{
    public required int Exchange { get; init; }
    public required long PriceOne { get; init; }

    /// <summary>
    /// When the sale ends, in Unix seconds.
    /// </summary>
    public required long ExpiresAt { get; init; }

    public required byte NoCommission { get; init; }
    public required ExchangeGoodsValue Goods { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Exchange);
        writer.Write(PriceOne);
        writer.Write(ExpiresAt);
        writer.Write(NoCommission);
        Goods.Write(writer);
    }
}
