namespace OpenWorker.Gameplay.Modules.Exchange;

/// <summary>
/// The messages of the exchange: numbers of the UI strings of the client. Where the original answers with a number the
/// client has no text for, the text decides.
/// </summary>
public static class ExchangeCodes
{
    /// <summary>The bags have no such storage.</summary>
    public const int WrongStorage = 52001;

    /// <summary>The item is not what the request says (also: the trade password is not given).</summary>
    public const int Mismatch = 52002;

    /// <summary>No such item in the tables.</summary>
    public const int NoItem = 52004;

    /// <summary>The item cannot go: not in the slot, worn down, or the listing is not in the right state.</summary>
    public const int ItemError = 52011;

    /// <summary>A count the item cannot have.</summary>
    public const int BadCount = 52013;

    /// <summary>A count the listing does not have.</summary>
    public const int CountMismatch = 52014;

    /// <summary>Not enough gold for the fee or the purchase.</summary>
    public const int NotEnoughGold = 52288;

    /// <summary>The item may not be traded: bound, or broken.</summary>
    public const int NotTradable = 52405;

    /// <summary>The total price of a listing is past 999,999,999.</summary>
    public const int PriceTooHigh = 16857;

    /// <summary>The player sells it.</summary>
    public const int OwnItem = 16860;

    /// <summary>A sale needs a price.</summary>
    public const int NoPrice = 58301;

    /// <summary>As many listings on sale as the player may have.</summary>
    public const int SaleFull = 58302;

    /// <summary>As many waiting listings as the player may have.</summary>
    public const int WaitingFull = 58303;

    /// <summary>The sale has ended.</summary>
    public const int Expired = 58304;

    /// <summary>Sold out or taken back by its seller.</summary>
    public const int Gone = 58305;

    /// <summary>The list of the items the player keeps an eye on is full.</summary>
    public const int InterestFull = 58306;
}

/// <summary>
/// The rules of the exchange, as the original server has them.
/// </summary>
public static class ExchangeRules
{
    /// <summary>Listings on one page of a search.</summary>
    public const int PageSize = 10;

    /// <summary>The sales the price window shows.</summary>
    public const int SalesShown = 10;

    /// <summary>Items a player may keep an eye on.</summary>
    public const int InterestLimit = 10;

    /// <summary>Listings a player may have waiting.</summary>
    public const int WaitingLimit = 10;

    /// <summary>Listings a player may have on sale; a booster adds to it.</summary>
    public const int SaleLimit = 10;

    /// <summary>Listings a player may have on sale with the ticket for more.</summary>
    public const int SaleLimitWithTicket = 100;

    /// <summary>The most a listing may cost in all, as the client allows it.</summary>
    public const long TotalPriceLimit = 999_999_999;

    /// <summary>The share of a sale the exchange keeps, in percent.</summary>
    public const byte SaleRate = 9;

    /// <summary>The level max the search uses when the window names no kind of item.</summary>
    public const int AnyLevelMax = 68;

    /// <summary>The ticket that takes the commission of the sale away.</summary>
    public const int CommissionTicket = 834000008;

    /// <summary>The ticket that allows <see cref="SaleLimitWithTicket"/> listings.</summary>
    public const int CountTicket = 834000009;

    /// <summary>The ticket that makes a sale run <see cref="TicketExtension"/> longer.</summary>
    public const int ExpireTicket = 834000010;

    /// <summary>How long a sale runs.</summary>
    public static TimeSpan Duration { get; } = TimeSpan.FromHours(24);

    /// <summary>What one step of the booster that makes sales run longer adds.</summary>
    public static TimeSpan BoosterExtension { get; } = TimeSpan.FromHours(24);

    /// <summary>What the ticket that makes a sale run longer adds.</summary>
    public static TimeSpan TicketExtension { get; } = TimeSpan.FromHours(48);

    /// <summary>
    /// The steps of the booster from which the ticket that makes a sale run longer is not needed.
    /// </summary>
    public const int BoosterExtensionEnough = 4;

    /// <summary>
    /// The gold the seller gets for a sale: what the buyer paid, less the share of the exchange (rounded down).
    /// </summary>
    public static long SellerGets(long total, byte saleRate)
    {
        return total - total * Math.Min(saleRate, (byte)100) / 100;
    }

    /// <summary>
    /// The share of the exchange for a sale: none with a ticket or the booster that takes it away, else the rate less what
    /// the booster that lowers it takes off.
    /// </summary>
    public static byte RateOf(bool ticket, bool freeBooster, float reduction)
    {
        if (ticket || freeBooster)
        {
            return 0;
        }

        return (byte)Math.Clamp(SaleRate - (int)reduction, 0, SaleRate);
    }

    /// <summary>
    /// How long a sale runs: a day, a day more for each step of the booster, two more with the ticket.
    /// </summary>
    public static TimeSpan DurationOf(int boosterSteps, bool ticket)
    {
        return Duration + BoosterExtension * Math.Max(0, boosterSteps) + (ticket ? TicketExtension : TimeSpan.Zero);
    }

    /// <summary>
    /// What the search asks for, as the original reads the window: a search for one item or for a kind of item keeps the
    /// filters; one for nothing in particular drops them all.
    /// </summary>
    public static Domain.Stores.ExchangeSearch Filter(int item, int category, int subCategory, int useClass, int levelMin, int levelMax,
        int gradeMin, int gradeMax, long price, int upgrade)
    {
        if (item <= 0 && (category < 0 || subCategory < 0))
        {
            return new Domain.Stores.ExchangeSearch(0, -1, -1, -1, -1, AnyLevelMax, -1, -1, -1, -1);
        }

        // A grade from that is not set leaves the grade open.
        if (gradeMin < 0)
        {
            gradeMax = -1;
        }

        return item > 0
            ? new Domain.Stores.ExchangeSearch(item, -1, -1, useClass, levelMin, levelMax, gradeMin, gradeMax, price, upgrade)
            : new Domain.Stores.ExchangeSearch(0, category, subCategory, useClass, levelMin, levelMax, gradeMin, gradeMax, price, upgrade);
    }

    /// <summary>
    /// The pages a search has: at least one.
    /// </summary>
    public static int PagesOf(int total)
    {
        return Math.Max(1, (total + PageSize - 1) / PageSize);
    }
}
