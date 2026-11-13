namespace OpenWorker.Domain.Persistent;

/// <summary>
/// How many of a limited product of the cash shop an account, or one of its characters, bought in one period.
/// </summary>
public sealed class CashShopPurchasePersistent
{
    public int AccountId { get; init; }

    /// <summary>
    /// The character that bought, or 0 when the account counts.
    /// </summary>
    public int PersonId { get; init; }

    public int Product { get; init; }

    /// <summary>
    /// When the period of the count started (UTC); one fixed moment for a count for good.
    /// </summary>
    public DateTime Period { get; init; }

    public int Count { get; set; }
}

/// <summary>
/// A list of products the cash shop keeps for a character: the basket (0) or a named set (1 to 8).
/// </summary>
public sealed class CashShopSetPersistent
{
    public const int MaxNameLength = 10;
    public const int Size = 10;

    public int PersonId { get; init; }

    public byte SetNo { get; init; }

    public required string Name { get; set; }

    /// <summary>
    /// The products, 0 for an empty place.
    /// </summary>
    public int[] Products { get; set; } = new int[Size];

    /// <summary>
    /// The item of the price chosen at each place.
    /// </summary>
    public int[] Items { get; set; } = new int[Size];
}
