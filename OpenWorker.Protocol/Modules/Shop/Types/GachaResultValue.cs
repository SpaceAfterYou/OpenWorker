using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Shop.Types;

/// <summary>
/// An item a gacha gave, and the kind of draw it was (the column of the table it came from).
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct GachaResultValue
{
    public required int Item { get; init; }
    public required int Type { get; init; }
}
