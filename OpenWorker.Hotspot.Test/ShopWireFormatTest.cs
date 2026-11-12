using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Extensions;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.Hotspot.Modules.Shop.Responses;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The answers of the shops as the client reads them: the purchase ends with the price and a one-byte currency, the sale and
/// the buying back are an int-counted list of slots and items, what a shop would sell back a byte-counted list of items.
/// </summary>
[TestClass]
public sealed class ShopWireFormatTest
{
    private static byte[] ToBytes(Action<BinaryWriter> write)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        write(writer);
        writer.Flush();

        return stream.ToArray();
    }

    [TestMethod]
    public void ShopAnswersHaveTheLayoutsOfTheClient()
    {
        var item = new ItemValue
        {
            Item = 7,
            OptionList = Enumerable.Range(0, 5).Select(_ => new ItemOption(0, 0)).ToList(),
            BroachState = "000000000000000"
        };
        var itemSize = ToBytes(writer => WireBridge.Write(writer, item)).Length;
        var slot = new StorageItemValue(StorageGroup.Common, 3, item);

        var bought = ToBytes(new ShopBuyResponse
        {
            StorageList = [new StorageValue { Storage = StorageGroup.Common, Slot = 3, Item = item }],
            Spent = 1080,
            Currency = ShopCurrency.Gold
        }.Write);

        Assert.HasCount(4 + 1 + 2 + itemSize + 8 + 1, bought, "the currency of a purchase is one byte");
        Assert.AreEqual((byte)ShopCurrency.Gold, bought[^1]);

        Assert.HasCount(4 + 1 + 2 + itemSize, ToBytes(new ShopSellResponse { Items = [slot] }.Write));
        Assert.HasCount(4 + 1 + 2 + itemSize, ToBytes(new ShopRepurchaserResponse { Items = [slot] }.Write));
        Assert.HasCount(1 + 2 * itemSize, ToBytes(new ShopRepurchaserListResponse { Items = [item, item] }.Write));
        Assert.HasCount(1 + itemSize, ToBytes(new ShopRepurchaserAddResponse { Items = [item] }.Write));
    }
}
