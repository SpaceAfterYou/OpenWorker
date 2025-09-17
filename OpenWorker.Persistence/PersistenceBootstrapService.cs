using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OpenWorker.Persistence;

internal sealed class PersistenceBootstrapService( 
    ILogger<PersistenceBootstrapService> logger,
    IDbContextFactory<PersistenceContext> factory
) : 
    BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await BootstrapPersistenceAsync(cancellationToken).ConfigureAwait(false);
    }
    
    private async ValueTask BootstrapPersistenceAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Bootstrapping persistence...");
        
        await using var context = await factory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        
        logger.LogDebug("Migrating database...");
        
        await context.Database
            .MigrateAsync(cancellationToken)
            .ConfigureAwait(false);
        
        logger.LogDebug("Seeding database...");
        
        await context
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
        
        logger.LogDebug("Database bootstrapped.");
    }
}