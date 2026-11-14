using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Persistence;
using StackExchange.Redis;

namespace OpenWorker.DependencyInjection;

/// <summary>
/// Fills <see cref="ServerContent"/> from the database when the server starts.<br/>
/// The host builds every hosted service before it starts the first, so a read in a constructor or a factory of the
/// container would run before the migration of this process and could meet a table that does not exist yet. This
/// service starts after the migration (it goes first, see <c>AddPersistenceBootstrapService</c>) and before the
/// listener of the game, so no client sees the contents unloaded. A table that is still missing then stops the server.
/// It listens to the switches of the other servers first (a server without Redis has none), so none falls between.
/// </summary>
internal sealed partial class ServerContentLoadService(
    ServerContent contents,
    IDbContextFactory<PersistenceContext> factory,
    ILogger<ServerContentLoadService> logger,
    IServiceProvider services
) :
    IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (services.GetService<IConnectionMultiplexer>() is not null)
        {
            var changes = services.GetRequiredService<ServerContentChanges>();

            try
            {
                await changes.ListenAsync();
            }

            catch (Exception e) when (e is not OperationCanceledException)
            {
                // The server runs with the table as it reads it now.
                LogNotListening(e);
            }
        }

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

    [LoggerMessage(Level = LogLevel.Error, Message = "The switches of the server contents are not heard: a switch made on another server does not reach this one.")]
    private partial void LogNotListening(Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Server contents loaded: {Count} rows.")]
    private partial void LogLoaded(int count);
}
