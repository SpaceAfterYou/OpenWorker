using OpenWorker.Extensions;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;
using System.Diagnostics;

namespace OpenWorker.Hotspot.Modules.Post.Types;

public readonly struct PostSendValue : IWritableData
{
    public string TargetName { get; init; }
    public string Title { get; init; }
    public string Message { get; init; }
    public long Money { get; init; }
    public PostSendItemValue[] Items { get; init; }

    public PostSendValue(BinaryReader reader)
    {
        TargetName = reader.ReadPersonName();
        Title = reader.ReadUtf8UnicodeString(PostModuleDefines.MaxTitleLength);
        Message = reader.ReadUtf8UnicodeString(PostModuleDefines.MaxMessageLength);
        Money = reader.ReadInt64();
        Items = Enumerable
            .Range(0, PostModuleDefines.ItemCount)
            .Select(_ => new PostSendItemValue(reader))
            .ToArray();
    }

    public void Write(BinaryWriter writer)
    {
        Debug.Assert(Items.Length is PostModuleDefines.ItemCount);

        writer.WritePersonName(TargetName);
        writer.WriteUtf16UnicodeString(Title, PostModuleDefines.MaxTitleLength);
        writer.WriteUtf16UnicodeString(Message, PostModuleDefines.MaxMessageLength);
        writer.Write(Money);

        foreach (var item in Items)
        {
            item.Write(writer);
        }
    }
}
