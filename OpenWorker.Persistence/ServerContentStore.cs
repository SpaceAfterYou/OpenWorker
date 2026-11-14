using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;

namespace OpenWorker.Persistence;

/// <summary>
/// The contents of the servers over the database, one row per content.
/// </summary>
public sealed class ServerContentStore(IDbContextFactory<PersistenceContext> factory) : IServerContentStore
{
    public async Task SetAsync(ServerContentIndex index, bool state, CancellationToken cancellationToken = default)
    {
        // A second server that makes the same row at once loses on the key and writes it again as an update.
        for (var attempt = 0; ; attempt++)
        {
            await using var database = await factory.CreateDbContextAsync(cancellationToken);

            var row = await database.ServerContents.FirstOrDefaultAsync(e => e.Index == index, cancellationToken);

            if (row is null)
            {
                database.ServerContents.Add(new ServerContentPersistent { Index = index, State = state });
            }

            else
            {
                row.State = state;
                row.UpdatedAt = DateTime.UtcNow;
            }

            try
            {
                await database.SaveChangesAsync(cancellationToken);

                return;
            }

            catch (DbUpdateException) when (row is null && attempt == 0)
            {
                // The row is there now: the next round updates it.
            }
        }
    }
}
