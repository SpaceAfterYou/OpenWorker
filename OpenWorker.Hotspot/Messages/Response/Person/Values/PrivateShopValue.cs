using OpenWorker.Extensions;
using OpenWorker.Hotspot.Messages.Response.Person.Components;
using OpenWorker.Hotspot.Modules.Shop.Components;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

public readonly struct PrivateShopValue
{
    public byte Type { get; }
    public string Name { get; }

    public PrivateShopValue(ShopPrivateComponent component)
    {
        Type = component.Type;
        Name = component.Name;
    }

    public PrivateShopValue(BinaryReader reader)
    {
        Type = reader.ReadByte();
        Name = reader.ReadUtf8UnicodeString();
    }
}