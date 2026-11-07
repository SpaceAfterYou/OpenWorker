using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Extensions;
using OpenWorker.Hotspot.Modules.Items.Types;

namespace OpenWorker.Hotspot.Modules.Trade.Types;

public readonly struct PrivateShopItemValue : IWritableData
{
    public ItemValue Item { get; init; }
    public long Money { get; init; }

    public PrivateShopItemValue(BinaryReader reader)
    {
        Item = new ItemValue(reader);
        Money = reader.ReadInt64();
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Item);
        writer.Write(Money);
    }
}
