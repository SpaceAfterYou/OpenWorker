using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Shop.Types;

/// <summary>
/// A banner of the cash shop: the address of its picture (1 to 499 bytes, ASCII; the client refuses the message otherwise) and how
/// many seconds it stays.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct ShopBannerValue
{
    [WireString(WireStringFormat.Utf8AsciiWithoutTerminator)]
    public required string Url { get; init; }

    public required int Seconds { get; init; }
}
