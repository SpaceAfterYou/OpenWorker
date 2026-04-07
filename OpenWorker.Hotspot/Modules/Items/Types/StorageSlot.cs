using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Extensions;

namespace OpenWorker.Hotspot.Modules.Items.Types;

public readonly struct StorageSlot(BinaryReader reader) : IWritableData
{
    public StorageGroup Storage { get; init; } = reader.ReadStorageGroup();
    public short Slot { get; init; } = reader.ReadInt16();

    public void Write(BinaryWriter writer)
    {
        writer.Write(Storage);
        writer.Write(Slot);
    }
}
