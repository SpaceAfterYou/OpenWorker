using OpenWorker.Extensions;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

public readonly struct PrivateShopValue
{
    public byte Type { get; }
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
