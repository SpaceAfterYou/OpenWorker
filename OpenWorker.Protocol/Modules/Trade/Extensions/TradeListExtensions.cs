using OpenWorker.Hotspot.Modules.Trade.Types;

namespace OpenWorker.Hotspot.Modules.Trade.Extensions;

public static class TradeListExtensions
{
    public static void Write(this BinaryWriter writer, PrivateShopItemValue[] values)
    {
        writer.Write(checked((byte)values.Length));

        foreach (var value in values)
        {
            value.Write(writer);
        }
    }
}
