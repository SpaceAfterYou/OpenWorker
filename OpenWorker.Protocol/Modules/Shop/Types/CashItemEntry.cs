using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Shop.Types;

[WireMessage]
public readonly partial struct CashItemEntry
{
    public int Index { get; }
    public byte Select { get; }
}
