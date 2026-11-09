using Microsoft.EntityFrameworkCore;

namespace OpenWorker.Persistence;

public static class PersistenceContextOptions
{
    /// <summary>
    /// What every context of the servers shares, whatever the database: queries do not track what they read. Most of them
    /// read, and a store that updates loaded entities asks for tracking with <c>AsTracking()</c>.
    /// </summary>
    public static DbContextOptionsBuilder UsePersistenceDefaults(this DbContextOptionsBuilder builder)
    {
        return builder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
}
