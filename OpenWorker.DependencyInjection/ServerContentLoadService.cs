using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenWorker.Hotspot;
using OpenWorker.Persistence;

namespace OpenWorker.DependencyInjection;

/// <summary>
/// Fills <see cref="ServerContent"/> from the database when the server starts.<br/>
/// The host builds every hosted service before it starts the first, so a read in a constructor or a factory of the
/// container would run before the migration of this process and could meet a table that does not exist yet. This
/// service starts after the migration (it goes first, see <c>AddPersistenceBootstrapService</c>) and before the
/// listener of the game, so no client sees the contents unloaded. A table that is still missing then stops the server.
/// </summary>
internal sealed partial class ServerContentLoadService(
    ServerContent contents,
    IDbContextFactory<PersistenceContext> factory,
    ILogger<ServerContentLoadService> logger
) :
    IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        var rows = await database.ServerContents
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        contents.Load(rows);

        LogLoaded(rows.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Server contents loaded: {Count} rows.")]
    private partial void LogLoaded(int count);
}
