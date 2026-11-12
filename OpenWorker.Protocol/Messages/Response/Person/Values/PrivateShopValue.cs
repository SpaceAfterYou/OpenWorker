using OpenWorker.Extensions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

[WireMessage(WireMethods.Write)]
public readonly partial struct PrivateShopValue : IWireReadable<PrivateShopValue>
{
    public byte Type { get; }
    [WireString(WireStringFormat.Utf16Unicode)]
    public string Name { get; }

    public PrivateShopValue(byte type, string name)
    {
        Type = type;
        Name = name;
    }

    /// <summary>
    /// What the client sends of a new character: the name of the shop counted in bytes, not in characters as it is written.
    /// </summary>
    public static PrivateShopValue Read(ref PacketReader reader)
    {
        var type = reader.ReadByte();
        var name = reader.ReadUtf8UnicodeString();

        return new PrivateShopValue(type, name);
    }
}
