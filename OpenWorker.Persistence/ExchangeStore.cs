using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;

namespace OpenWorker.Persistence;

/// <summary>
/// The exchange over the database. A purchase is one statement whose condition says what the listing must still be (on
/// sale, not ended, holding the count, not the buyer's, at the price shown), so two buyers on two servers cannot both take
/// the same item; a limit of a seller is counted in a transaction that holds the row of the seller; a listing taken back
/// is held for the rest of its transaction, so a purchase that comes meanwhile waits and then finds it gone. The letters a
/// change writes go in the same transaction as the change.
/// </summary>
public sealed class ExchangeStore(IDbContextFactory<PersistenceContext> factory, TimeProvider? time = null) : IExchangeStore
{
    private DateTime Now => (time ?? TimeProvider.System).GetUtcNow().UtcDateTime;

    #region The items a person keeps an eye on

    public async Task<List<int>> ListInterestsAsync(int person, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        return await database.ExchangeInterests
            .Where(e => e.PersonId == person)
            .OrderBy(e => e.CreatedAt)
            .Select(e => e.Item)
            .ToListAsync(cancellationToken);
    }

    public async Task<ExchangeInterestOutcome> AddInterestAsync(int person, int item, int limit, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);

        await RowLocks.HoldPersonsAsync(database, person, person);

        var items = database.ExchangeInterests.Where(e => e.PersonId == person);

        if (await items.AnyAsync(e => e.Item == item, cancellationToken))
        {
            return ExchangeInterestOutcome.Already;
        }

        if (await items.CountAsync(cancellationToken) >= limit)
        {
            return ExchangeInterestOutcome.Full;
        }

        database.ExchangeInterests.Add(new ExchangeInterestPersistent { PersonId = person, Item = item, CreatedAt = Now });

        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return ExchangeInterestOutcome.Already;
        }

        await transaction.CommitAsync(cancellationToken);

        return ExchangeInterestOutcome.Added;
    }

    public async Task<bool> RemoveInterestAsync(int person, int item, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        return await database.ExchangeInterests
            .Where(e => e.PersonId == person && e.Item == item)
            .ExecuteDeleteAsync(cancellationToken) > 0;
    }

    #endregion

    #region The listings of a seller

    public async Task<List<ExchangeListingPersistent>> ListMineAsync(int person, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        return await database.ExchangeListings
            .Where(e => e.SellerId == person)
            .OrderBy(e => e.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<ExchangeListingPersistent?> FindAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        return await database.ExchangeListings.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<ExchangeListingResult> AddWaitingAsync(ExchangeListingDraft draft, int limit, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);

        await RowLocks.HoldPersonsAsync(database, draft.Seller, draft.Seller);

        var waiting = await database.ExchangeListings
            .CountAsync(e => e.SellerId == draft.Seller && e.State == ExchangeListingState.Waiting, cancellationToken);

        if (waiting >= limit)
        {
            return new ExchangeListingResult(ExchangeOutcome.Full, null);
        }

        var now = Now;

        var listing = new ExchangeListingPersistent
        {
            SellerId = draft.Seller,
            SellerName = draft.SellerName,
            SellerHero = draft.SellerHero,
            Item = draft.Goods.Item,
            Goods = draft.Goods,
            InitialCount = draft.Goods.Count,
            Count = draft.Goods.Count,
            State = ExchangeListingState.Waiting,
            Group = draft.Group,
            SubGroup = draft.SubGroup,
            UseClass = draft.UseClass,
            Level = draft.Level,
            Rank = draft.Rank,
            Upgrade = draft.Goods.Upgrade,
            CreatedAt = now,
            UpdatedAt = now
        };

        database.ExchangeListings.Add(listing);

        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ExchangeListingResult(ExchangeOutcome.Done, listing);
    }

    public async Task<ExchangeListingResult> PutOnSaleAsync(
        long serial,
        int seller,
        long priceOne,
        byte saleRate,
        DateTime expiresAt,
        int limit,
        CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);

        await RowLocks.HoldPersonsAsync(database, seller, seller);

        var listing = await database.ExchangeListings
            .AsTracking()
            .FirstOrDefaultAsync(e => e.Serial == serial && e.SellerId == seller, cancellationToken);

        if (listing is null)
        {
            return new ExchangeListingResult(ExchangeOutcome.Missing, null);
        }

        if (listing.State != ExchangeListingState.Waiting)
        {
            return new ExchangeListingResult(ExchangeOutcome.WrongState, listing);
        }

        var onSale = await database.ExchangeListings
            .CountAsync(e => e.SellerId == seller && e.State == ExchangeListingState.OnSale, cancellationToken);

        if (onSale >= limit)
        {
            return new ExchangeListingResult(ExchangeOutcome.Full, listing);
        }

        listing.State = ExchangeListingState.OnSale;
        listing.PriceOne = priceOne;
        listing.SaleRate = saleRate;
        listing.ExpiresAt = expiresAt;
        listing.UpdatedAt = Now;

        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ExchangeListingResult(ExchangeOutcome.Done, listing);
    }

    public async Task<ExchangeRecallResult> RecallAsync(
        long serial,
        int seller,
        Func<ExchangeListingPersistent, PostPersistent> letter,
        CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);

        // Held until the end: a purchase of it waits, and finds it gone.
        await database.Database.ExecuteSqlAsync(
            $"""SELECT 1 FROM "ExchangeListings" WHERE "Serial" = {serial} AND "SellerId" = {seller} FOR UPDATE""",
            cancellationToken);

        var listing = await database.ExchangeListings
            .AsTracking()
            .FirstOrDefaultAsync(e => e.Serial == serial && e.SellerId == seller, cancellationToken);

        if (listing is null)
        {
            return new ExchangeRecallResult(ExchangeOutcome.Missing, null, null);
        }

        var written = letter(listing);

        database.ExchangeListings.Remove(listing);
        database.Posts.Add(written);

        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ExchangeRecallResult(ExchangeOutcome.Done, listing, written);
    }

    #endregion

    #region Buyers

    public async Task<(int Total, List<ExchangeListingPersistent> Page)> SearchAsync(
        ExchangeSearch search,
        int page,
        int pageSize,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        var listings = database.ExchangeListings
            .Where(e => e.State == ExchangeListingState.OnSale && e.Count > 0 && e.ExpiresAt > now);

        if (search.Item > 0)
        {
            listings = listings.Where(e => e.Item == search.Item);
        }

        if (search.Group >= 0)
        {
            listings = listings.Where(e => e.Group == search.Group);
        }

        if (search.SubGroup >= 0)
        {
            listings = listings.Where(e => e.SubGroup == search.SubGroup);
        }

        // An item for every character matches any character.
        if (search.UseClass >= 0)
        {
            listings = listings.Where(e => e.UseClass == search.UseClass || e.UseClass == 0);
        }

        if (search.LevelMin >= 0)
        {
            listings = listings.Where(e => e.Level >= search.LevelMin);
        }

        if (search.LevelMax >= 0)
        {
            listings = listings.Where(e => e.Level <= search.LevelMax);
        }

        if (search.RankMin >= 0)
        {
            listings = listings.Where(e => e.Rank >= search.RankMin);
        }

        if (search.RankMax >= 0)
        {
            listings = listings.Where(e => e.Rank <= search.RankMax);
        }

        if (search.PriceMax >= 0)
        {
            listings = listings.Where(e => e.PriceOne <= search.PriceMax);
        }

        if (search.UpgradeMin >= 0)
        {
            listings = listings.Where(e => e.Upgrade >= search.UpgradeMin);
        }

        var total = await listings.CountAsync(cancellationToken);

        var found = await listings
            .OrderBy(e => e.PriceOne)
            .ThenBy(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (total, found);
    }

    public async Task<ExchangeBuyResult> BuyAsync(
        int id,
        int buyer,
        int item,
        short count,
        long priceOne,
        DateTime now,
        ExchangeSaleLetters letters,
        CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);

        // The one statement that decides: of two buyers of the last item, the second finds the count gone.
        var taken = await database.ExchangeListings
            .Where(e => e.Id == id &&
                        e.Item == item &&
                        e.State == ExchangeListingState.OnSale &&
                        e.ExpiresAt > now &&
                        e.Count >= count &&
                        e.SellerId != buyer &&
                        e.PriceOne == priceOne)
            .ExecuteUpdateAsync(e => e
                .SetProperty(x => x.Count, x => x.Count - count)
                .SetProperty(x => x.UpdatedAt, now), cancellationToken);

        if (taken == 0)
        {
            var listing = await database.ExchangeListings.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

            return new ExchangeBuyResult(Refusal(listing, buyer, item, count, priceOne, now), listing, null, null);
        }

        var sold = await database.ExchangeListings
            .AsTracking()
            .FirstAsync(e => e.Id == id, cancellationToken);

        var (toBuyer, toSeller) = letters(sold, count);

        database.Posts.Add(toBuyer);
        database.Posts.Add(toSeller);
        database.ExchangeSales.Add(new ExchangeSalePersistent { Item = sold.Item, Count = count, PriceOne = priceOne, SoldAt = now });

        // Sold out: it leaves the list of the seller.
        if (sold.Count == 0)
        {
            database.ExchangeListings.Remove(sold);
        }

        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ExchangeBuyResult(ExchangeOutcome.Done, sold, toBuyer, toSeller);
    }

    public async Task<List<ExchangeSalePersistent>> ListSalesAsync(int item, int count, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        return await database.ExchangeSales
            .Where(e => e.Item == item)
            .OrderByDescending(e => e.SoldAt)
            .ThenByDescending(e => e.Id)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Why a purchase found nothing to take, read after the statement that decided.
    /// </summary>
    private static ExchangeOutcome Refusal(ExchangeListingPersistent? listing, int buyer, int item, short count, long priceOne, DateTime now)
    {
        if (listing is null)
        {
            return ExchangeOutcome.Missing;
        }

        if (listing.SellerId == buyer)
        {
            return ExchangeOutcome.Own;
        }

        if (listing.State != ExchangeListingState.OnSale || listing.Item != item || listing.PriceOne != priceOne)
        {
            return ExchangeOutcome.Changed;
        }

        if (listing.ExpiresAt <= now)
        {
            return ExchangeOutcome.Expired;
        }

        return listing.Count < count ? ExchangeOutcome.NotEnough : ExchangeOutcome.Changed;
    }

    #endregion
}
