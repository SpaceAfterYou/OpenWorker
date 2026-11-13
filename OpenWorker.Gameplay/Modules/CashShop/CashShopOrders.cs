using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Post;
using OpenWorker.Hotspot.Modules.Shop.Types;

namespace OpenWorker.Gameplay.Modules.CashShop;

/// <summary>
/// Who an order is for: the buyer itself, or the character a present goes to.
/// </summary>
public readonly record struct CashBuyer(int Person, int Account, string Name, byte Hero);

/// <summary>
/// An order checked against the tables: the price, the counts of the limited products and what it gives.
/// </summary>
public sealed record CashOrder(long Price, IReadOnlyList<CashLimitDraft> Limits, IReadOnlyList<PostItemPersistent> Items);

/// <summary>
/// The checks of an order of the cash shop, in the order the original makes them, and the letters that carry what it
/// gives. The balance and the counts are decided by the store, in the transaction of the purchase.
/// </summary>
public sealed class CashShopOrders(CashShopCatalog catalog, ItemCatalog items)
{
    /// <summary>
    /// The most products of one order.
    /// </summary>
    public const int MaxEntries = 10;

    // The rows of the client's table of system letters: a purchase, and a present.
    private const byte BoughtLetter = 8;
    private const byte GiftLetter = 17;

    // The kinds of item the shop gives: the cash bag, and the cube.
    private const byte CashBag = 13;
    private const byte Cube = 9;

    /// <summary>
    /// Who a letter of a purchase is from; the client shows the sender of its row.
    /// </summary>
    public const string Sender = "Item Shop";

    // The kind of item that opens an appearance.
    private const byte Appearance = 19;

    public CashShopCatalog Catalog => catalog;

    /// <summary>
    /// Checks an order and says what it costs and gives, or the code it is refused with. A present is checked against its
    /// receiver's class and may hold only the products that may be given.
    /// </summary>
    public (CashOrder? Order, int Code) Check(IReadOnlyList<CashItemEntry> entries, CashBuyer buyer, bool gift, DateTimeOffset now)
    {
        var refused = gift ? CashShopCodes.NotSold : CashShopCodes.WrongProduct;

        if (entries.Count is 0 or > MaxEntries)
        {
            return (null, CashShopCodes.WrongProduct);
        }

        long price = 0;
        var counts = new Dictionary<int, (CashProduct Product, int Count)>();
        var given = new List<PostItemPersistent>();

        foreach (var entry in entries)
        {
            if (!catalog.TryGet(entry.Index, out var product) || entry.Select >= product.Choices.Count)
            {
                return (null, refused);
            }

            if (!product.OnSale || gift && !product.Gift)
            {
                return (null, CashShopCodes.NotSold);
            }

            if (!product.InPeriod(now.DateTime))
            {
                return (null, CashShopCodes.OutOfPeriod);
            }

            var choice = product.Choices[entry.Select];

            if (!items.TryGet(choice.Item, out var row) || items.Classify(choice.Item) is not { } classify)
            {
                return (null, refused);
            }

            if (row.ClassLimit is > 0 and < 100 && row.ClassLimit != buyer.Hero)
            {
                return (null, gift ? CashShopCodes.ReceiverClass : CashShopCodes.OtherClass);
            }

            // Appearances are not kept by this server; anything else goes to the cash bag or the cube.
            if (classify.Group == Appearance || classify.StorageKind is not (CashBag or Cube))
            {
                return (null, refused);
            }

            if (product.Limit != CashLimitKind.None)
            {
                counts[product.Index] = (product, counts.TryGetValue(product.Index, out var counted) ? counted.Count + 1 : 1);
            }

            price += choice.Price;
            given.Add(new PostItemPersistent { Item = choice.Item, Count = choice.Count, Endurance = 100 });
        }

        var limits = counts.Values
            .Select(e => new CashLimitDraft(
                e.Product.Index,
                CashLimits.ByAccount(e.Product.Limit),
                CashLimits.PeriodOf(e.Product.Limit, now),
                e.Count,
                e.Product.LimitCount))
            .ToList();

        return (new CashOrder(price, limits, given), 0);
    }

    /// <summary>
    /// The letters that carry what an order gives, five items to a letter: the letter of a purchase, or of a present from
    /// <paramref name="from"/>.
    /// </summary>
    public static List<PostPersistent> Letters(CashOrder order, CashBuyer to, string? from, DateTime now)
    {
        var letters = new List<PostPersistent>();

        for (var first = 0; first < order.Items.Count; first += PostPersistent.MaxItems)
        {
            letters.Add(new PostPersistent
            {
                Kind = PostKind.Person,
                SenderName = from ?? Sender,
                ReceiverId = to.Person,
                ReceiverName = to.Name,
                ReceiverHero = to.Hero,
                Title = string.Empty,
                Body = string.Empty,
                Items = order.Items.Skip(first).Take(PostPersistent.MaxItems).ToList(),
                MainType = GameLetters.PersonKind,
                SubType = from is null ? BoughtLetter : GiftLetter,
                State = PostState.None,
                CreatedAt = now,
                UpdatedAt = now,
                ExpiresAt = now + PostLimits.Lifetime
            });
        }

        return letters;
    }
}
