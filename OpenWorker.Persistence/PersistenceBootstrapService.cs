using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using OpenWorker.Extensions;

namespace OpenWorker.Persistence;

/// <summary>
/// Has the database schema up to date before the server starts. The host waits for
/// <see cref="StartAsync"/>, so nothing that starts after this service meets a missing table.
/// One server of an installation migrates (<see cref="PersistenceOptions.Migrate"/>, the relay); the others
/// wait here until it has: servers that all migrate an empty Postgres make the database and its first
/// tables at once, and all but one fail.
/// </summary>
internal sealed partial class PersistenceBootstrapService(
    ILogger<PersistenceBootstrapService> logger,
    IDbContextFactory<PersistenceContext> factory,
    IOptions<PersistenceOptions> options
) :
    IHostedService
{
    private static TimeSpan Pause { get; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How many looks pass between two warnings that the schema is still not there.
    /// </summary>
    private const int LooksPerWarning = 30;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LogBootstrapping();

        if (options.Value.Migrate)
        {
            await using var context = await factory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            LogMigrating();

            await context.Database
                .MigrateAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        else
        {
            await WaitForSchemaAsync(cancellationToken).ConfigureAwait(false);
        }

        LogBootstrapped();
    }

    /// <summary>
    /// Looks every second until no migration is left to apply. A database that is not there yet, or does not answer,
    /// is one more reason to look again.
    /// </summary>
    private async Task WaitForSchemaAsync(CancellationToken cancellationToken)
    {
        for (var look = 0; ; look++)
        {
            try
            {
                await using var context = await factory
                    .CreateDbContextAsync(cancellationToken)
                    .ConfigureAwait(false);

                var pending = await context.Database
                    .GetPendingMigrationsAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (!pending.Any())
                {
                    return;
                }
            }
            catch (NpgsqlException)
            {
                // No database yet, or no answer: the same as a schema that is not there.
            }

            if (look % LooksPerWarning == 0)
            {
                LogWaiting();
            }

            await Task.Delay(Pause, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Bootstrapping persistence...")]
    private partial void LogBootstrapping();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Migrating database...")]
    private partial void LogMigrating();

    [LoggerMessage(Level = LogLevel.Warning, Message = "The database schema is not up to date. Waiting for the server that migrates it (the relay, or any server started with Persistence:Migrate=true).")]
    private partial void LogWaiting();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Database bootstrapped.")]
    private partial void LogBootstrapped();
}
