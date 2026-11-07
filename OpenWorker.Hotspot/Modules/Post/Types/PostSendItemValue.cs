using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Types;

namespace OpenWorker.Hotspot.Modules.Post.Types;

public readonly struct PostSendItemValue : IWritableData
{
    public StorageSlot Slot { get; init; }
    public short Count { get; init; }

    public PostSendItemValue(BinaryReader reader)
    {
        Slot = new StorageSlot(reader);
        Count = reader.ReadInt16();
    }

    public void Write(BinaryWriter writer)
    {
        Slot.Write(writer);
        writer.Write(Count);
    }
}
