using System.Collections.ObjectModel;
using System.Globalization;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.Hotspot.Modules.Shop.Types;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.CashShop;

/// <summary>
/// One choice of a product: the item it gives, how many, the price before the sale and the price paid.
/// </summary>
public readonly record struct CashChoice(int Item, short Count, int BasePrice, int Price);

/// <summary>
/// A product of the cash shop as the client's <c>tb_CashShop</c> describes it, with the choices its prices allow.
/// </summary>
public readonly record struct CashProduct(
    int Index,
    int Item,
    bool OnSale,
    bool Gift,
    byte Level,
    DateTime? Start,
    DateTime? End,
    CashLimitKind Limit,
    short LimitCount,
    IReadOnlyList<CashChoice> Choices)
{
    /// <summary>
    /// Whether the product is sold at <paramref name="now"/> (the local time of the server): a product without a period
    /// always is, one with a period between its start and its end.
    /// </summary>
    public bool InPeriod(DateTime now)
    {
        return (Start is not { } start || start <= now) && (End is not { } end || now <= end);
    }
}

/// <summary>
/// The products of the cash shop. The client's table says what is sold and how; the prices come from the server's own
/// <c>tb_CashBilling_Info</c> under the same rows, where the client's has every price 0. A choice is kept while the item
/// and count of both tables agree and the price is not 0, and the choices stop at the first that is not kept, as the client
/// counts them from the first. A product with no choice left, or whose item no table knows, is not sold.
/// </summary>
public sealed class CashShopCatalog
{
    /// <summary>
    /// Products in one part of the list (09/20), as the original sends them.
    /// </summary>
    public const int PartSize = 200;

    private static readonly DateTimeOffset NoDate = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly RowIndex<int, CashProduct> _products;

    public CashShopCatalog(
        ReadOnlyCollection<CashShopRow> products,
        ReadOnlyCollection<CashBillingInfoRow> clientPrices,
        ReadOnlyCollection<CashBillingRow> prices,
        ItemCatalog items)
    {
        var client = clientPrices.IndexBy(e => (int)e.Id);
        var server = prices.IndexBy(e => (int)e.Id);
        var sold = new List<CashProduct>();
        var listing = new List<CashItemValue>();

        for (var order = 0; order < products.Count; order++)
        {
            var row = products[order];

            if (!items.Exists(row.Item) || !client.TryGet(row.Billing, out var shown) || !server.TryGet(row.Billing, out var priced))
            {
                continue;
            }

            var choices = Choices(shown, priced, items);

            if (choices.Count == 0)
            {
                continue;
            }

            var product = new CashProduct(
                row.Id,
                row.Item,
                row.SellActive == 1,
                row.Gift == 1,
                row.Level,
                row.LimitType == 1 ? ParseDate(row.LimitStart) : null,
                row.LimitType == 1 ? ParseDate(row.LimitEnd) : null,
                Enum.IsDefined((CashLimitKind)row.NumberType) ? (CashLimitKind)row.NumberType : CashLimitKind.None,
                row.NumberValue,
                choices);

            sold.Add(product);
            listing.Add(Listed(product, row, order));
        }

        _products = sold.IndexBy(e => e.Index);
        Products = sold;
        Listing = listing;
    }

    public IReadOnlyList<CashProduct> Products { get; }

    /// <summary>
    /// The products as the window lists them, in the order of the table.
    /// </summary>
    public IReadOnlyList<CashItemValue> Listing { get; }

    public bool TryGet(int index, out CashProduct product)
    {
        return _products.TryGet(index, out product);
    }

    private static List<CashChoice> Choices(CashBillingInfoRow shown, CashBillingRow priced, ItemCatalog items)
    {
        int[] shownItems = [shown.Item1, shown.Item2, shown.Item3, shown.Item4, shown.Item5];
        int[] shownCounts = [shown.Count1, shown.Count2, shown.Count3, shown.Count4, shown.Count5];
        var choices = new List<CashChoice>();

        for (var index = 0; index < CashBillingRow.Choices; index++)
        {
            var item = priced.Items[index];
            var count = priced.Counts[index];

            if (item == 0 || item != shownItems[index] || count != shownCounts[index] || count is <= 0 or > short.MaxValue ||
                priced.Prices[index] <= 0 || !items.Exists(item) || count > items.StackLimit(item))
            {
                break;
            }

            choices.Add(new CashChoice(item, (short)count, Math.Max(priced.BasePrices[index], priced.Prices[index]), priced.Prices[index]));
        }

        return choices;
    }

    private static CashItemValue Listed(CashProduct product, CashShopRow row, int order)
    {
        var marks = (CashShopMarks)0;

        if (row.LimitType == 1)
        {
            marks |= CashShopMarks.BuyLimit;
        }

        if (product.Limit != CashLimitKind.None)
        {
            marks |= CashShopMarks.BuyCount;
        }

        if (product.Gift)
        {
            marks |= CashShopMarks.IconGift;
        }

        return new CashItemValue
        {
            Index = product.Index,
            SellActive = row.SellActive,
            Item = product.Item,
            Level = product.Level,
            Order = order,
            NeedSlot = row.NeedSlot,
            CashInfo = (short)marks,
            LimitTime1 = product.Start is { } start ? new DateTimeOffset(start, TimeZoneInfo.Local.GetUtcOffset(start)) : NoDate,
            LimitTime2 = product.End is { } end ? new DateTimeOffset(end, TimeZoneInfo.Local.GetUtcOffset(end)) : NoDate,
            SellCount = product.Limit == CashLimitKind.None ? (byte)0 : (byte)Math.Clamp((int)product.LimitCount, 0, byte.MaxValue),
            Billing = row.Billing,
            CashInfoList = product.Choices
                .Select(e => new CashInfoValue { Item = e.Item, Count = e.Count, BasePrice = e.BasePrice, Price = e.Price })
                .ToList()
        };
    }

    /// <summary>
    /// A moment of the table, "yyyy-mm-dd hh:mm:ss" with the time or its seconds left out at will; "0" or anything else that
    /// is not a date is none.
    /// </summary>
    public static DateTime? ParseDate(string text)
    {
        var parts = text.Split([' ', '-', ':'], StringSplitOptions.RemoveEmptyEntries);
        var values = new int[6];

        if (parts.Length < 3)
        {
            return null;
        }

        for (var index = 0; index < parts.Length && index < values.Length; index++)
        {
            if (!int.TryParse(parts[index], NumberStyles.None, CultureInfo.InvariantCulture, out values[index]))
            {
                return null;
            }
        }

        if (values[0] is < 2000 or > 2100 || values[1] is < 1 or > 12 || values[2] < 1 || values[2] > DateTime.DaysInMonth(values[0], values[1]) ||
            values[3] > 23 || values[4] > 59 || values[5] > 59)
        {
            return null;
        }

        return new DateTime(values[0], values[1], values[2], values[3], values[4], values[5], DateTimeKind.Unspecified);
    }
}
