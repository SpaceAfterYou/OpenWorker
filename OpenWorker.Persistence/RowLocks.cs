using Microsoft.EntityFrameworkCore;

namespace OpenWorker.Persistence;

/// <summary>
/// Holds rows for the rest of a transaction, so a limit that is counted and then written is counted by one request at a
/// time: the next one waits and counts what the first wrote. Persons are held in the order of their numbers, so two
/// requests about the same two persons never wait for each other.
/// </summary>
internal static class RowLocks
{
    public static Task HoldPersonsAsync(PersistenceContext database, int first, int second)
    {
        var low = Math.Min(first, second);
        var high = Math.Max(first, second);

        return database.Database.ExecuteSqlAsync(
            $"""SELECT 1 FROM "Persons" WHERE "Id" IN ({low}, {high}) ORDER BY "Id" FOR UPDATE""");
    }

    public static Task HoldLeagueAsync(PersistenceContext database, int league)
    {
        return database.Database.ExecuteSqlAsync($"""SELECT 1 FROM "Leagues" WHERE "Id" = {league} FOR UPDATE""");
    }
}
