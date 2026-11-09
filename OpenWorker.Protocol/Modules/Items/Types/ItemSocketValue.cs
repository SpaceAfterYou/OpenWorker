using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Items.Types;

public readonly struct ItemSocketValue : IWritableData
{
    public long EquipSerial { get; init; }
    public int Socket { get; init; }
    public byte Position { get; init; }
    public List<ItemOption> Options { get; init; }

    public ItemSocketValue(BinaryReader reader)
    {
        EquipSerial = reader.ReadInt64();
        Socket = reader.ReadInt32();
        Position = reader.ReadByte();
        Options = Enumerable
            .Range(0, ItemModuleDefines.OptionCount)
            .Select(_ => new ItemOption(reader))
            .ToList();
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(EquipSerial);
        writer.Write(Socket);
        writer.Write(Position);
        writer.Write(Options);
    }
}
