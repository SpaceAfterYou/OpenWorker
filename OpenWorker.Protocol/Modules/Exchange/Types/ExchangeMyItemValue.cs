using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Exchange.Types;

/// <summary>
/// A listing of the player as its own list shows it. The client keeps the entries by the serial of the item: an entry
/// with a count of 0 takes the listing off the list, any other replaces or adds it.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct ExchangeMyItemValue
{
    public required int Exchange { get; init; }

    /// <summary>
    /// When the sale ends, in Unix seconds; 0 while the listing waits.
    /// </summary>
    public required long ExpiresAt { get; init; }

    /// <summary>
    /// 1 waiting, 2 on sale (shown as expired once <see cref="ExpiresAt"/> is past).
    /// </summary>
    public required byte State { get; init; }

    public required short InitialCount { get; init; }
    public required long PriceOne { get; init; }
    public required ExchangeGoodsValue Goods { get; init; }
}
