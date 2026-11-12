using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Post.Types;

/// <summary>
/// The character that sent a letter: who, its class and its name.
/// </summary>
/// <remarks>Written by the generator. The reader of <see cref="BinaryReader"/> stays by hand for the tests that read a
/// letter back: it takes the length of the name in bytes, which no one <c>[WireString]</c> says together with how it is
/// written.</remarks>
[WireMessage(WireMethods.Write)]
public readonly partial struct PostCharacterValue : IWritableData
{
    public int Character { get; init; }
    public byte Class { get; init; }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string Name { get; init; }

    public PostCharacterValue(BinaryReader reader)
    {
        Character = reader.ReadInt32();
        Class = reader.ReadByte();
        Name = reader.ReadPersonName();
    }
}
