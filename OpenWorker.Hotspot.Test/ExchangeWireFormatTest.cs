using OpenWorker.Hotspot.Modules.Exchange.Responses;
using OpenWorker.Hotspot.Modules.Exchange.Types;
using OpenWorker.Hotspot.Modules.Items.Types;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The two lists of the exchange that carry items, in the layouts client 1.4.2.3 reads: the search (page, pages, entries of
/// listing, price, end, commission mark and the item) and the list of the player (listing, end, state, first count, price
/// and the item). The client reads neither the package list nor the seller name the original wrote after the item, nor a
/// trailing "last" byte after the list of the player.
/// </summary>
[TestClass]
public sealed class ExchangeWireFormatTest
{
    // The item as every list writes it (TradePostWireFormatTest), then no gems, then the broaches: a serial and fifteen items.
    private const int ItemSize = 4 + 8 + 2 + 1 + 5 * (2 + 4) + 3 + 8 + 3 + 4 + (2 + 15) + 2;
    private const int GoodsSize = ItemSize + 1 + 8 + 15 * 4;

    private static ExchangeGoodsValue Goods()
    {
        return new ExchangeGoodsValue
        {
            Item = new ItemValue
            {
                Item = 900_000_001,
                Count = 3,
                OptionList = Enumerable.Range(0, 5).Select(_ => new ItemOption(0, 0)).ToList(),
                BroachState = "000000000000000"
            },
            Sockets = [],
            Broach = new ItemBroachValue { Serial = 42, Items = new int[ItemBroachValue.ItemCount] }
        };
    }

    private static byte[] ToBytes(Action<BinaryWriter> write)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        write(writer);
        writer.Flush();

        return stream.ToArray();
    }

    [TestMethod]
    public void TheSearchHasTheClientLayout()
    {
        var bytes = WireBytes.Of(new ExchangeSearchResponse
        {
            Page = 2,
            PageMax = 5,
            Items = [new ExchangeItemValue { Exchange = 7, PriceOne = 1_000, ExpiresAt = 1_800_000_000, NoCommission = 1, Goods = Goods() }]
        });

        Assert.HasCount(4 + 4 + 2 + 4 + 8 + 8 + 1 + GoodsSize, bytes);
        Assert.AreEqual(2, BitConverter.ToInt32(bytes, 0));
        Assert.AreEqual(5, BitConverter.ToInt32(bytes, 4));
        Assert.AreEqual(1, BitConverter.ToInt16(bytes, 8));
        Assert.AreEqual(7, BitConverter.ToInt32(bytes, 10));
        Assert.AreEqual(1_000, BitConverter.ToInt64(bytes, 14));
        Assert.AreEqual(1_800_000_000, BitConverter.ToInt64(bytes, 22));
        Assert.AreEqual(1, bytes[30]);
        Assert.AreEqual(900_000_001, BitConverter.ToInt32(bytes, 31), "the item follows the commission mark");
        Assert.AreEqual(0, bytes[31 + ItemSize], "no gems");
        Assert.AreEqual(42, BitConverter.ToInt64(bytes, 31 + ItemSize + 1), "the serial of the broaches");
    }

    [TestMethod]
    public void TheListOfThePlayerHasTheClientLayout()
    {
        var bytes = WireBytes.Of(new ExchangeMyListResponse
        {
            Items = [new ExchangeMyItemValue { Exchange = 7, ExpiresAt = 1_800_000_000, State = 2, InitialCount = 5, PriceOne = 1_000, Goods = Goods() }]
        });

        Assert.HasCount(2 + 4 + 8 + 1 + 2 + 8 + GoodsSize, bytes, "no trailing byte after the list");
        Assert.AreEqual(1, BitConverter.ToInt16(bytes, 0));
        Assert.AreEqual(7, BitConverter.ToInt32(bytes, 2));
        Assert.AreEqual(1_800_000_000, BitConverter.ToInt64(bytes, 6));
        Assert.AreEqual(2, bytes[14]);
        Assert.AreEqual(5, BitConverter.ToInt16(bytes, 15));
        Assert.AreEqual(1_000, BitConverter.ToInt64(bytes, 17));
        Assert.AreEqual(900_000_001, BitConverter.ToInt32(bytes, 25));
        Assert.AreEqual(3, BitConverter.ToInt16(bytes, 25 + 12), "the count that is left, after the item and its serial");
    }
}
