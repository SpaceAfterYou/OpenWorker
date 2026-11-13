using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;

namespace OpenWorker.Persistence;

/// <summary>
/// The balance of an account, the counts of limited products and the lists of the cash shop. A purchase is one transaction:
/// one statement takes the price only while the balance holds it, one statement per limited product adds to its count only
/// while the count stays within the limit, and the letters with what was bought are written with them.
/// </summary>
public sealed class CashShopStore(IDbContextFactory<PersistenceContext> factory) : ICashShopStore
{
    public async Task<long> BalanceAsync(int account, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        return await database.Accounts
            .Where(e => e.Id == account)
            .Select(e => e.Cash)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<long?> GiveAsync(int account, long amount, long limit, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        var balances = await database.Database
            .SqlQuery<long>($"""
                UPDATE "Accounts" SET "Cash" = "Cash" + {amount}
                WHERE "Id" = {account} AND "Cash" + {amount} >= 0 AND "Cash" + {amount} <= {limit}
                RETURNING "Cash" AS "Value"
                """)
            .ToArrayAsync(cancellationToken);

        return balances.Length == 0 ? null : balances[0];
    }

    public async Task<CashPurchaseResult> BuyAsync(CashPurchaseDraft draft, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);

        var balances = await database.Database
            .SqlQuery<long>($"""
                UPDATE "Accounts" SET "Cash" = "Cash" - {draft.Price}
                WHERE "Id" = {draft.Account} AND "Cash" >= {draft.Price}
                RETURNING "Cash" AS "Value"
                """)
            .ToArrayAsync(cancellationToken);

        if (balances.Length == 0)
        {
            return new CashPurchaseResult(CashPurchaseOutcome.NotEnough, await Balance(database, draft.Account, cancellationToken));
        }

        foreach (var limit in draft.Limits)
        {
            if (limit.Count > limit.Limit)
            {
                await transaction.RollbackAsync(cancellationToken);

                return new CashPurchaseResult(CashPurchaseOutcome.Limit, await Balance(database, draft.Account, cancellationToken));
            }

            var person = limit.ByAccount ? 0 : draft.Buyer;

            // A count already at the limit is left as it is, and the statement returns nothing.
            var counts = await database.Database
                .SqlQuery<int>($"""
                    INSERT INTO "CashShopPurchases" ("AccountId", "PersonId", "Product", "Period", "Count")
                    VALUES ({draft.Account}, {person}, {limit.Product}, {limit.Period}, {limit.Count})
                    ON CONFLICT ("AccountId", "PersonId", "Product", "Period")
                    DO UPDATE SET "Count" = "CashShopPurchases"."Count" + EXCLUDED."Count"
                    WHERE "CashShopPurchases"."Count" + EXCLUDED."Count" <= {limit.Limit}
                    RETURNING "Count" AS "Value"
                    """)
                .ToArrayAsync(cancellationToken);

            if (counts.Length == 0)
            {
                await transaction.RollbackAsync(cancellationToken);

                return new CashPurchaseResult(CashPurchaseOutcome.Limit, await Balance(database, draft.Account, cancellationToken));
            }
        }

        database.Posts.AddRange(draft.Letters);

        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new CashPurchaseResult(CashPurchaseOutcome.Done, balances[0]);
    }

    public async Task<CashReceiver?> FindPersonAsync(string name, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        var key = name.Trim().ToLowerInvariant();

        // The comparison runs in the database, as lower("Name") = @key, as the post finds a receiver.
#pragma warning disable CA1304, CA1311, CA1862
        var found = await database.Persons
            .Where(e => e.Name.ToLower() == key)
            .Select(e => new { e.Id, AccountId = EF.Property<int>(e, "AccountId"), e.Name, e.Hero })
            .Take(1)
            .ToListAsync(cancellationToken);
#pragma warning restore CA1304, CA1311, CA1862

        return found.Count == 0 ? null : new CashReceiver(found[0].Id, found[0].AccountId, found[0].Name, (byte)found[0].Hero);
    }

    public async Task<List<CashShopSetPersistent>> ListSetsAsync(int person, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        return await database.CashShopSets
            .Where(e => e.PersonId == person)
            .OrderBy(e => e.SetNo)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveSetAsync(CashShopSetPersistent list, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        await database.Database.ExecuteSqlAsync($"""
            INSERT INTO "CashShopSets" ("PersonId", "SetNo", "Name", "Products", "Items")
            VALUES ({list.PersonId}, {list.SetNo}, {list.Name}, {list.Products}, {list.Items})
            ON CONFLICT ("PersonId", "SetNo")
            DO UPDATE SET "Name" = EXCLUDED."Name", "Products" = EXCLUDED."Products", "Items" = EXCLUDED."Items"
            """, cancellationToken);
    }

    public async Task<bool> DeleteSetAsync(int person, byte setNo, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        return await database.CashShopSets
            .Where(e => e.PersonId == person && e.SetNo == setNo)
            .ExecuteDeleteAsync(cancellationToken) > 0;
    }

    private static Task<long> Balance(PersistenceContext database, int account, CancellationToken cancellationToken)
    {
        return database.Accounts
            .Where(e => e.Id == account)
            .Select(e => e.Cash)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
