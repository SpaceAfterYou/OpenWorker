using OpenWorker.Extensions;
using OpenWorker.Hotspot.Modules.Items.Extensions;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Post.Types;

/// <summary>
/// A letter between characters: who sent it, its texts, its gold and its <see cref="PostModuleDefines.ItemCount"/>
/// items, its times, and the gems and broaches of the items.
/// </summary>
/// <remarks>Written by the generator. The reader of <see cref="BinaryReader"/> stays by hand for the tests that read a
/// letter back: it takes the lengths of the texts in bytes, which no one <c>[WireString]</c> says together with how they
/// are written.</remarks>
[WireMessage(WireMethods.Write)]
public readonly partial struct PostValue
{
    public long Serial { get; init; }
    public PostCharacterValue Character { get; init; }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string Title { get; init; }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string Message { get; init; }

    public long Money { get; init; }

    [WireArray(Count = PostModuleDefines.ItemCount)]
    public ItemValue[] Items { get; init; }

    public long RegisteredTime { get; init; }
    public byte Flag { get; init; }
    public byte Type { get; init; }
    public byte SubType { get; init; }
    public long RemainTime { get; init; }
    public long Event { get; init; }

    [WireArray(WireLength.Byte)]
    public ItemSocketValue[] Sockets { get; init; }

    [WireArray(WireLength.Byte)]
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
}
