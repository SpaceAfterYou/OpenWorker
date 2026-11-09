using OpenWorker.Extensions;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.Post.Types;

public readonly struct PostCharacterValue : IWritableData
{
    public int Character { get; init; }
    public byte Class { get; init; }
    public string Name { get; init; }

    public PostCharacterValue(BinaryReader reader)
    {
        Character = reader.ReadInt32();
        Class = reader.ReadByte();
        Name = reader.ReadPersonName();
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Character);
        writer.Write(Class);
        writer.WritePersonName(Name);
    }
}
