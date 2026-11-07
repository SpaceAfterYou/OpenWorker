using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenWorker.Hotspot;

namespace OpenWorker.Channel.Community;

/// <summary>
/// Opens the way to the other servers and keeps the people of this one on the list for as long as they stay.
/// </summary>
internal sealed class CommunityService(
    CommunityNetwork network,
    WorldLoop loop,
    IEnumerable<ICommunityModule> modules,
    ILogger<CommunityService> logger
) :
    BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        foreach (var module in modules)
        {
            module.Attach(network);
        }

        try
        {
            await network.StartAsync();
        }

        catch (Exception e)
        {
            // Without it the server still serves its own people.
            logger.LogError(e, "Could not start talking to the other servers.");
        }

        using var timer = new PeriodicTimer(CommunityNetwork.PresenceRenewal);

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await loop.InvokeAsync(() => network.RenewAsync());
            }
        }

        catch (OperationCanceledException)
        {
        }
    }
}
