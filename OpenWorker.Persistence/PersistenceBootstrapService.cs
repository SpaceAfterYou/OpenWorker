using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OpenWorker.Persistence;

/// <summary>
/// Brings the database schema up to date before the server starts. The host waits for
/// <see cref="StartAsync"/>, so nothing that starts after this service meets a missing table.
/// Every server runs it: the provider locks the database while it migrates, so servers that
/// start together apply each migration once.
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

        await using var context = await factory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        LogMigrating();

        await context.Database
            .MigrateAsync(cancellationToken)
            .ConfigureAwait(false);

        LogBootstrapped();
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
