using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Shop.Types;

/// <summary>
/// A slot of the bags whose whole stack is sold.
/// </summary>
[WireMessage]
public readonly partial struct ShopSellEntry
{
    public ShopSellEntry(StorageGroup storage, short slot)
    {
        Storage = storage;
        Slot = slot;
    }

    public StorageGroup Storage { get; }
    public short Slot { get; }
}
