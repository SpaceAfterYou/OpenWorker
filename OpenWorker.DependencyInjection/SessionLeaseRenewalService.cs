using Microsoft.Extensions.Logging;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache;

namespace OpenWorker.DependencyInjection;

/// <summary>
/// Renews the leases of the sessions this server holds. When the server drains it stops first, so that a renewal in
/// flight cannot write a lease as connected after the close of its session has written it as disconnected;
/// the close itself is <see cref="SessionLeaseManager.OnSessionClosedAsync"/>.
/// </summary>
internal sealed partial class SessionLeaseRenewalService(
    SessionLeaseManager leases,
    ILogger<SessionLeaseRenewalService> logger) : RegistrationService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SessionLeaseManager.RenewInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await leases.RenewAsync();
            }
            catch (Exception exception)
            {
                LogRenewFailed(logger, exception);
            }
        }
    }

    /// <summary>
    /// The leases are not records of the server but of its sessions, and each session releases its own as it closes.
    /// </summary>
    protected override Task RemoveAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to renew session leases.")]
    private static partial void LogRenewFailed(ILogger logger, Exception exception);
}
