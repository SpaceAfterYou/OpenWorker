using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenWorker.Domain.Persistent;

namespace OpenWorker.Persistence;

public enum SupportRegisterOutcome
{
    Registered,

    /// <summary>
    /// The reward of the last orb is still to be taken.
    /// </summary>
    RewardWaiting,

    /// <summary>
    /// The last orb can still be used.
    /// </summary>
    StillValid
}

public enum SupportUseOutcome
{
    Used,

    /// <summary>
    /// The friend offers no orb, or is not a friend.
    /// </summary>
    Missing,

    /// <summary>
    /// The day of the orb is over.
    /// </summary>
    Expired,

    /// <summary>
    /// The person used this orb already.
    /// </summary>
    AlreadyUsed
}

/// <summary>
/// An orb a friend offers, as the list of a person shows it: the friend, its name, level and class, the orb.
/// </summary>
public readonly record struct SupportOffer(int Friend, string Name, byte Level, byte Hero, byte SupportType, float Value, DateTime Until);

/// <summary>
/// The orbs persons offer their friends for their helpers, and who used which. Every server reads and writes the same rows: an
/// orb is offered once per person (the key), used once per friend (a unique index), and its reward claimed once (a conditional
/// update).
/// </summary>
public static class HelperSupportStore
{
    public static async Task<HelperSupportPersistent?> SupportOfAsync(this IDbContextFactory<PersistenceContext> factory, int person)
    {
        await using var database = await factory.CreateDbContextAsync();

        return await database.HelperSupports.AsNoTracking().FirstOrDefaultAsync(e => e.PersonId == person);
    }

    /// <summary>
    /// Offers a new orb, unless the last one can still be used or its reward is still to be taken. Who used the old orbs is
    /// forgotten with them.
    /// </summary>
    public static async Task<SupportRegisterOutcome> RegisterSupportAsync(
        this IDbContextFactory<PersistenceContext> factory,
        int person,
        byte supportType,
        float value,
        DateTime until,
        DateTime now)
    {
        await using var database = await factory.CreateDbContextAsync();
        await using var transaction = await database.Database.BeginTransactionAsync();

        // One statement decides: a new row, or the old one replaced only when its day is over and its reward taken.
        var written = await database.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "HelperSupports" ("PersonId", "SupportType", "Value", "Until", "Reward", "CreatedAt", "UpdatedAt")
            VALUES ({person}, {supportType}, {value}, {until}, {HelperSupportPersistent.RewardWaiting}, {now}, {now})
            ON CONFLICT ("PersonId") DO UPDATE SET "SupportType" = EXCLUDED."SupportType", "Value" = EXCLUDED."Value",
                "Until" = EXCLUDED."Until", "Reward" = EXCLUDED."Reward", "UpdatedAt" = EXCLUDED."UpdatedAt"
            WHERE "HelperSupports"."Until" <= {now} AND "HelperSupports"."Reward" <> {HelperSupportPersistent.RewardWaiting}
            """);

        if (written == 0)
        {
            var old = await database.HelperSupports.AsNoTracking().FirstAsync(e => e.PersonId == person);

            return old.Reward == HelperSupportPersistent.RewardWaiting ? SupportRegisterOutcome.RewardWaiting : SupportRegisterOutcome.StillValid;
        }

        await database.HelperSupportUses.Where(e => e.SupporterId == person && e.Until != until).ExecuteDeleteAsync();

        await transaction.CommitAsync();

        return SupportRegisterOutcome.Registered;
    }

    /// <summary>
    /// Takes the reward of the orb a person offers: true once, false when there is none waiting.
    /// </summary>
    public static async Task<bool> ClaimSupportRewardAsync(this IDbContextFactory<PersistenceContext> factory, int person, DateTime now)
    {
        await using var database = await factory.CreateDbContextAsync();

        return await database.HelperSupports
            .Where(e => e.PersonId == person && e.Reward == HelperSupportPersistent.RewardWaiting)
            .ExecuteUpdateAsync(e => e
                .SetProperty(x => x.Reward, HelperSupportPersistent.RewardTaken)
                .SetProperty(x => x.UpdatedAt, now)) == 1;
    }

    /// <summary>
    /// Gives a claimed reward back, when the items could not be given after all.
    /// </summary>
    public static async Task ReturnSupportRewardAsync(this IDbContextFactory<PersistenceContext> factory, int person)
    {
        await using var database = await factory.CreateDbContextAsync();

        await database.HelperSupports
            .Where(e => e.PersonId == person && e.Reward == HelperSupportPersistent.RewardTaken)
            .ExecuteUpdateAsync(e => e.SetProperty(x => x.Reward, HelperSupportPersistent.RewardWaiting));
    }

    /// <summary>
    /// The orbs the friends of a person offer that can still be used and that the person has not used.
    /// </summary>
    public static async Task<List<SupportOffer>> SupportOffersAsync(this IDbContextFactory<PersistenceContext> factory, int person, DateTime now)
    {
        await using var database = await factory.CreateDbContextAsync();

        return await database.Friends
            .AsNoTracking()
            .Where(e => e.OwnerId == person && e.Kind == FriendKind.Friend)
            .Join(database.HelperSupports, e => e.TargetId, e => e.PersonId, (friend, support) => support)
            .Where(e => e.Until > now &&
                        !database.HelperSupportUses.Any(x => x.SupporterId == e.PersonId && x.UserId == person && x.Until == e.Until))
            .Join(database.Persons, e => e.PersonId, e => e.Id, (support, friend) => new SupportOffer(
                friend.Id, friend.Name, friend.Level, (byte)friend.Hero, support.SupportType, support.Value, support.Until))
            .ToListAsync();
    }

    /// <summary>
    /// A person puts the orb of a friend on a helper: the orb must be offered by a friend, still valid and not used by the person
    /// yet. The friend earns its points with the person for it, in the same transaction.
    /// </summary>
    public static async Task<(SupportUseOutcome Outcome, SupportOffer Offer)> UseSupportAsync(
        this IDbContextFactory<PersistenceContext> factory,
        int person,
        int friend,
        int friendPoints,
        DateTime now)
    {
        await using var database = await factory.CreateDbContextAsync();
        await using var transaction = await database.Database.BeginTransactionAsync();

        var offers = await database.Friends
            .AsNoTracking()
            .Where(e => e.OwnerId == person && e.TargetId == friend && e.Kind == FriendKind.Friend)
            .Join(database.HelperSupports, e => e.TargetId, e => e.PersonId, (_, support) => support)
            .Join(database.Persons, e => e.PersonId, e => e.Id, (support, supporter) => new SupportOffer(
                supporter.Id, supporter.Name, supporter.Level, (byte)supporter.Hero, support.SupportType, support.Value, support.Until))
            .ToListAsync();

        if (offers.Count == 0)
        {
            return (SupportUseOutcome.Missing, default);
        }

        var offer = offers[0];

        if (offer.Until <= now)
        {
            return (SupportUseOutcome.Expired, offer);
        }

        database.HelperSupportUses.Add(new HelperSupportUsePersistent { SupporterId = friend, UserId = person, Until = offer.Until });

        try
        {
            await database.SaveChangesAsync();
        }

        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return (SupportUseOutcome.AlreadyUsed, offer);
        }

        if (friendPoints > 0)
        {
            await database.Friends
                .Where(e => e.OwnerId == friend && e.TargetId == person && e.Kind == FriendKind.Friend)
                .ExecuteUpdateAsync(e => e.SetProperty(x => x.Point, x => x.Point + friendPoints));
        }

        await transaction.CommitAsync();

        return (SupportUseOutcome.Used, offer);
    }
}
