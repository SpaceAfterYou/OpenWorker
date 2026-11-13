using System.Text;
using OpenWorker.Hotspot.Modules.Shop.Responses;
using OpenWorker.Hotspot.Modules.Shop.Types;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The answers of the cash shop in the layouts client 1.4.2.3 reads: a part of the list (a "last part" byte, then the
/// products, each with the count of its prices first), a banner (a 16-bit count of bytes, the address, the seconds), a
/// purchase (byte-counted appearances and failed products), a list of the character and the draw of a gacha.
/// </summary>
[TestClass]
public sealed class CashShopWireFormatTest
{
    [TestMethod]
    public void TheCashShopAnswersHaveTheClientLayouts()
    {
        var part = WireBytes.Of(new ShopCashLoadResponse
        {
            Last = true,
            ItemList =
            [
                new CashItemValue
                {
                    Index = 9, SellActive = 1, Item = 837_000_044, Level = 0, Order = 3, NeedSlot = 1, CashInfo = 8,
                    LimitTime1 = DateTimeOffset.FromUnixTimeSeconds(946_684_800), LimitTime2 = DateTimeOffset.FromUnixTimeSeconds(946_684_800),
                    SellCount = 0, Billing = 9, CashInfoList = [new CashInfoValue { Item = 837_000_044, Count = 1, BasePrice = 300, Price = 250 }]
                }
            ]
        });

        Assert.HasCount(1 + 4 + (1 + 4 + 1 + 4 + 2 + 4 + 1 + 2 + 8 + 8 + 1 + 4) + (4 + 2 + 4 + 4), part);
        Assert.AreEqual(1, part[0], "the last part");
        Assert.AreEqual(1, BitConverter.ToInt32(part, 1));
        Assert.AreEqual(1, part[5], "the count of the prices comes first");
        Assert.AreEqual(9, BitConverter.ToInt32(part, 6));
        Assert.AreEqual(250, BitConverter.ToInt32(part, part.Length - 4));

        var banner = WireBytes.Of(new ShopBannerLoadResponse { BannerList = [new ShopBannerValue { Url = "http://a/b.png", Seconds = 5 }] });

        CollectionAssert.AreEqual(
            new byte[] { 1, 0, 0, 0, 14, 0 }.Concat(Encoding.ASCII.GetBytes("http://a/b.png")).Concat(BitConverter.GetBytes(5)).ToArray(),
            banner);

        var bought = WireBytes.Of(new ShopCashBuyResponse
        {
            Appearances = [new CashAppearanceValue { Appearance = 12, EndsAt = 0 }],
            Failed = [9, 10]
        });

        CollectionAssert.AreEqual(
            new byte[] { 1, 12, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 9, 0, 0, 0, 10, 0, 0, 0 },
            bought);

        var set = WireBytes.Of(new ShopCashSetResponse
        {
            Set = new CashSetValue { SetNo = 2, Name = "ab", Products = [9], Items = [837_000_044] }
        });

        Assert.HasCount(1 + 2 + 2 * 2 + 1 + 10 * 4 + 10 * 4, set, "the name as the other names, then ten places of each list");
        Assert.AreEqual(2, set[0]);
        Assert.AreEqual(9, BitConverter.ToInt32(set, 8));
        Assert.AreEqual(837_000_044, BitConverter.ToInt32(set, 48));

        var drawn = WireBytes.Of(new ShopGachaResResponse { Results = [new GachaResultValue { Item = 7, Type = 2 }] });

        CollectionAssert.AreEqual(new byte[] { 1, 0, 0, 0, 7, 0, 0, 0, 2, 0, 0, 0 }, drawn);
    }
}
