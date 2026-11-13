using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Shop.Types;

/// <summary>
/// A list of products the cash shop keeps for a character: 0 is the basket, 1 to 8 the named sets. Each place holds the
/// product and the item of its price chosen, 0 when empty.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct CashSetValue
{
    public const int Size = 10;

    public required byte SetNo { get; init; }

    [WireString(WireStringFormat.Utf16Unicode)]
    public required string Name { get; init; }

    [WireArray(Count = Size)]
    public required IReadOnlyList<int> Products { get; init; }

    [WireArray(Count = Size)]
    public required IReadOnlyList<int> Items { get; init; }
}
