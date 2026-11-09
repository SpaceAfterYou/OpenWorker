using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Modules.Items.Enums;

namespace OpenWorker.Hotspot.Modules.Shop.Types;

/// <summary>
/// A slot of the bags whose whole stack is sold.
/// </summary>
public readonly struct ShopSellEntry
{
    public ShopSellEntry(StorageGroup storage, short slot)
    {
        Storage = storage;
        Slot = slot;
    }

    public ShopSellEntry(BinaryReader reader) : this(reader.ReadStorageGroup(), reader.ReadInt16())
    {
    }

    public StorageGroup Storage { get; }

    public short Slot { get; }
}
