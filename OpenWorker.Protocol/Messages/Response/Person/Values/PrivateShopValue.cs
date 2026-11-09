using OpenWorker.Extensions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

[WireMessage(WireMethods.Write)]
public readonly partial struct PrivateShopValue
{
    public byte Type { get; }
    [WireString(WireStringFormat.Utf16Unicode)]
    public string Name { get; }

    public PrivateShopValue(byte type, string name)
    {
        Type = type;
        Name = name;
    }

    public PrivateShopValue(BinaryReader reader)
    {
        Type = reader.ReadByte();
        Name = reader.ReadUtf8UnicodeString();
    }
}
