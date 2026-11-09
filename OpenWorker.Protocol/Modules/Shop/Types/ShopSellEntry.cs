using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Modules.Items.Enums;

namespace OpenWorker.Hotspot.Modules.Shop.Types;

public readonly struct ShopSellEntry(BinaryReader reader)
{
    public StorageGroup Storage { get; } = reader.ReadStorageGroup();
    public short Slot { get; } = reader.ReadInt16();
}