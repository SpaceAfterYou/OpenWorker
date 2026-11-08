using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenWorker.Hotspot;

namespace OpenWorker.Channel.Community;

/// <summary>
/// Opens the way to the other servers and keeps the people of this one on the list for as long as they stay.<br/>
/// A round that fails is logged and the next one runs: the list would expire after <see cref="CommunityNetwork.PresenceLifetime"/>
/// without them, and the people of this server would turn into strangers for the others.
/// </summary>
public sealed partial class CommunityService(
    CommunityNetwork network,
    WorldLoop loop,
    IEnumerable<ICommunityModule> modules,
    TimeProvider time,
    ILogger<CommunityService> logger
) :
    BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var module in modules)
        {
            module.Attach(network);
        }

        try
        {
            await network.StartAsync();
        }

        catch (Exception e) when (e is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
        {
            // Without it the server still serves its own people.
            LogStartFailed(logger, e);
        }

        using var timer = new PeriodicTimer(CommunityNetwork.PresenceRenewal, time);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await loop.InvokeAsync(() => network.RenewAsync());
                }

                catch (Exception e) when (e is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
                {
                    LogRenewFailed(logger, e);
                }
            }
        }

        catch (OperationCanceledException)
        {
            // The server is stopping.
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Could not start talking to the other servers.")]
    private static partial void LogStartFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Could not renew the people of this server on the list.")]
    private static partial void LogRenewFailed(ILogger logger, Exception exception);
}
