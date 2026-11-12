using OpenWorker.Extensions;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Persons.Extensions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Post.Types;

/// <summary>
/// A letter to the account: its texts, its gold and its <see cref="PostModuleDefines.ItemCount"/> items, its times and
/// the name of who sent it.
/// </summary>
/// <remarks>Written by the generator. The reader of <see cref="BinaryReader"/> stays by hand: it takes the lengths of the
/// texts in bytes, which no one <c>[WireString]</c> says together with how they are written.</remarks>
[WireMessage(WireMethods.Write)]
public readonly partial struct PostAccountValue : IWritableData
{
    public long Serial { get; init; }
    public int Account { get; init; }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string Title { get; init; }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string Message { get; init; }

    public long Money { get; init; }

    [WireArray(Count = PostModuleDefines.ItemCount)]
    public ItemValue[] Items { get; init; }

    public long RegisteredTime { get; init; }
    public long RemainTime { get; init; }
    public byte Flag { get; init; }

    [WireString(WireStringFormat.Utf16Unicode)]
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
}
