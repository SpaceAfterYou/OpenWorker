using OpenWorker.Extensions;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Extensions;
using OpenWorker.Hotspot.Modules.Items.Types;
using System.Diagnostics;

namespace OpenWorker.Hotspot.Modules.Post.Types;

public readonly struct PostValue : IWritableData
{
    public long Serial { get; init; }
    public PostCharacterValue Character { get; init; }
    public string Title { get; init; }
    public string Message { get; init; }
    public long Money { get; init; }
    public ItemValue[] Items { get; init; }
    public long RegisteredTime { get; init; }
    public byte Flag { get; init; }
    public byte Type { get; init; }
    public byte SubType { get; init; }
    public long RemainTime { get; init; }
    public long Event { get; init; }
    public ItemSocketValue[] Sockets { get; init; }
    public ItemBroachValue[] Broaches { get; init; }

    public PostValue(BinaryReader reader)
    {
        Serial = reader.ReadInt64();
        Character = new PostCharacterValue(reader);
        Title = reader.ReadUtf8UnicodeString(PostModuleDefines.MaxTitleLength);
        Message = reader.ReadUtf8UnicodeString(PostModuleDefines.MaxMessageLength);
        Money = reader.ReadInt64();
        Items = Enumerable
            .Range(0, PostModuleDefines.ItemCount)
            .Select(_ => new ItemValue(reader))
            .ToArray();
        RegisteredTime = reader.ReadInt64();
        Flag = reader.ReadByte();
        Type = reader.ReadByte();
        SubType = reader.ReadByte();
        RemainTime = reader.ReadInt64();
        Event = reader.ReadInt64();
        Sockets = reader.ReadItemSocketList();
        Broaches = reader.ReadItemBroachList();
    }

    public void Write(BinaryWriter writer)
    {
        Debug.Assert(Items.Length is PostModuleDefines.ItemCount);

        writer.Write(Serial);
        Character.Write(writer);
        writer.WriteUtf16UnicodeString(Title, PostModuleDefines.MaxTitleLength);
        writer.WriteUtf16UnicodeString(Message, PostModuleDefines.MaxMessageLength);
        writer.Write(Money);

        foreach (var item in Items)
        {
            writer.Write(item);
        }

        writer.Write(RegisteredTime);
        writer.Write(Flag);
        writer.Write(Type);
        writer.Write(SubType);
        writer.Write(RemainTime);
        writer.Write(Event);
        writer.Write(Sockets);
        writer.Write(Broaches);
    }
}
