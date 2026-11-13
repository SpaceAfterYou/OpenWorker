using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Shop.Types;

/// <summary>
/// A product the client asks for, and which of its prices it chose (0 to 4).
/// </summary>
[WireMessage]
public readonly partial struct CashItemEntry
{
    public int Index { get; }
    public byte Select { get; }
}
