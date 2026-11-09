using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Modules.Items.Enums;

namespace OpenWorker.Hotspot.Modules.Items.Requests;

public readonly struct MoveItemValue(BinaryReader reader)
{
    public StorageGroup Storage { get; init; } = reader.ReadStorageGroup();
    public int Item { get; init; } = reader.ReadInt32();
    public short Index { get; init; } = reader.ReadInt16();
}