using OpenWorker.Extensions;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Extensions;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Persons.Extensions;
using System.Diagnostics;

namespace OpenWorker.Hotspot.Modules.Post.Types;

public readonly struct PostAccountValue : IWritableData
{
    public long Serial { get; init; }
    public int Account { get; init; }
    public string Title { get; init; }
    public string Message { get; init; }
    public long Money { get; init; }
    public ItemValue[] Items { get; init; }
    public long RegisteredTime { get; init; }
    public long RemainTime { get; init; }
    public byte Flag { get; init; }
    public string SenderName { get; init; }
    public byte MainType { get; init; }
    public byte SubType { get; init; }

    public PostAccountValue(BinaryReader reader)
    {
        Serial = reader.ReadInt64();
        Account = reader.ReadInt32();
        Title = reader.ReadUtf8UnicodeString(PostModuleDefines.MaxTitleLength);
        Message = reader.ReadUtf8UnicodeString(PostModuleDefines.MaxMessageLength);
        Money = reader.ReadInt64();
        Items = Enumerable
            .Range(0, PostModuleDefines.ItemCount)
            .Select(_ => new ItemValue(reader))
            .ToArray();
        RegisteredTime = reader.ReadInt64();
        RemainTime = reader.ReadInt64();
        Flag = reader.ReadByte();
        SenderName = reader.ReadPersonName();
        MainType = reader.ReadByte();
        SubType = reader.ReadByte();
    }

    public void Write(BinaryWriter writer)
    {
        Debug.Assert(Items.Length is PostModuleDefines.ItemCount);

        writer.Write(Serial);
        writer.Write(Account);
        writer.WriteUtf16UnicodeString(Title, PostModuleDefines.MaxTitleLength);
        writer.WriteUtf16UnicodeString(Message, PostModuleDefines.MaxMessageLength);
        writer.Write(Money);

        foreach (var item in Items)
        {
            writer.Write(item);
        }

        writer.Write(RegisteredTime);
        writer.Write(RemainTime);
        writer.Write(Flag);
        writer.WritePersonName(SenderName);
        writer.Write(MainType);
        writer.Write(SubType);
    }
}
