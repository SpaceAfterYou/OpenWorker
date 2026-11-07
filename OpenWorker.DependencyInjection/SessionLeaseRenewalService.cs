using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenWorker.Hotspot.Cache;

namespace OpenWorker.DependencyInjection;

internal sealed class SessionLeaseRenewalService(
    SessionLeaseManager leases,
    ILogger<SessionLeaseRenewalService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SessionLeaseManager.RenewInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await leases.RenewAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to renew session leases.");
            }
        }
    }
}
