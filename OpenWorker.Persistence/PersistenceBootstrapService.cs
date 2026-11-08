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
internal sealed class PersistenceBootstrapService(
    ILogger<PersistenceBootstrapService> logger,
    IDbContextFactory<PersistenceContext> factory
) :
    IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Bootstrapping persistence...");

        await using var context = await factory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        logger.LogDebug("Migrating database...");

        await context.Database
            .MigrateAsync(cancellationToken)
            .ConfigureAwait(false);

        logger.LogDebug("Database bootstrapped.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
