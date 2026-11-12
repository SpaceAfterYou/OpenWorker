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
/// One server of an installation migrates (<see cref="PersistenceOptions.Migrate"/>, the relay) and is started
/// first: whatever starts the servers waits until it is ready. Any other server looks once and refuses to start
/// on a schema that is not up to date. Servers that all migrate an empty Postgres make the database and its first
/// tables at once, and all but one fail.
/// </summary>
internal sealed partial class PersistenceBootstrapService(
    ILogger<PersistenceBootstrapService> logger,
    IDbContextFactory<PersistenceContext> factory,
    IOptions<PersistenceOptions> options
) :
    IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LogBootstrapping();

        if (options.Value.Migrate)
        {
            await using var context = await factory
                .CreateDbContextAsync(cancellationToken);

            LogMigrating();

            await context.Database
                .MigrateAsync(cancellationToken);
        }

        else
        {
            await using var context = await factory
                .CreateDbContextAsync(cancellationToken);

            if (await IsBehindAsync(context, cancellationToken))
            {
                throw new InvalidOperationException(
                    "The database schema is not up to date. Start the relay first and wait until it is ready: it migrates the database. " +
                    "To run this server alone, start it with Persistence:Migrate=true (Persistence__Migrate=true in the environment).");
            }
        }

        LogBootstrapped();
    }

    /// <summary>
    /// Whether a migration is left to apply. A database that is not there yet is behind too.
    /// </summary>
    private static async Task<bool> IsBehindAsync(PersistenceContext context, CancellationToken cancellationToken)
    {
        try
        {
            var pending = await context.Database
                .GetPendingMigrationsAsync(cancellationToken);

            return pending.Any();
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.InvalidCatalogName)
        {
            return true;
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

    [LoggerMessage(Level = LogLevel.Debug, Message = "Database bootstrapped.")]
    private partial void LogBootstrapped();
}
