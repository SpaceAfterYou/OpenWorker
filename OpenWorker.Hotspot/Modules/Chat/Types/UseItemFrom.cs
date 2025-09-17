using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Modules.Items.Enums;

namespace OpenWorker.Hotspot.Modules.Chat.Types;

public readonly struct UseItemFrom(BinaryReader reader)
{
    public StorageGroup Storage { get; } = reader.ReadStorageGroup();
    public short Slot { get; } = reader.ReadInt16();
}