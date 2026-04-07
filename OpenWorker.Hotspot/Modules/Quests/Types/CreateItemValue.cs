using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Quests.Types;

public readonly struct CreateItemValue(BinaryReader reader) : IWritableData
{
    public int Item { get; init; } = reader.ReadInt32();
    public short Count { get; init; } = reader.ReadInt16();

    public void Write(BinaryWriter writer)
    {
        writer.Write(Item);
        writer.Write(Count);
    }
}
