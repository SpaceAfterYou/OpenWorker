using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace OpenWorker.Persistence;

/// <summary>
/// Brings the database schema up to date before the server starts. The host waits for
/// <see cref="StartAsync"/>, so nothing that starts after this service meets a missing table.
/// Every server runs it: the provider locks the database while it migrates, so servers that
/// start together apply each migration once. The lock does not cover a database that is not there yet:
/// servers that start together on an empty Postgres all try to make it and its first tables, one wins
/// and the others are told the thing exists. Those try again and find the work done.
/// </summary>
internal sealed partial class PersistenceBootstrapService(
    ILogger<PersistenceBootstrapService> logger,
    IDbContextFactory<PersistenceContext> factory
) :
    IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LogBootstrapping();

        LogMigrating();

        for (var attempt = 1; ; attempt++)
        {
            // A new context for every attempt: the connection of a failed one is not used again.
            await using var context = await factory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                await context.Database
                    .MigrateAsync(cancellationToken)
                    .ConfigureAwait(false);

                break;
            }
            catch (PostgresException e) when (attempt < Attempts && MadeByAnotherServer(e))
            {
                LogMigratedElsewhere(e.SqlState, attempt);

                await Task.Delay(Pause, cancellationToken).ConfigureAwait(false);
            }
        }

        LogBootstrapped();
    }

    private const int Attempts = 10;

    private static TimeSpan Pause { get; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The database, a table or a row of the history was made by another server between the look and the making.
    /// </summary>
    private static bool MadeByAnotherServer(PostgresException e)
    {
        return e.SqlState is PostgresErrorCodes.DuplicateDatabase
            or PostgresErrorCodes.DuplicateTable
            or PostgresErrorCodes.DuplicateObject
            or PostgresErrorCodes.UniqueViolation;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Bootstrapping persistence...")]
    private partial void LogBootstrapping();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Migrating database...")]
    private partial void LogMigrating();

    [LoggerMessage(Level = LogLevel.Information, Message = "Another server is migrating the database ({SqlState}), attempt {Attempt}; trying again.")]
    private partial void LogMigratedElsewhere(string sqlState, int attempt);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Database bootstrapped.")]
    private partial void LogBootstrapped();
}
