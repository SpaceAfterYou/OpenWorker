using Microsoft.Extensions.Hosting;
using OpenWorker.GameServer.Worlds.Abstractions;

namespace OpenWorker.GameServer.Worlds;

public sealed class WorldService(IWorld world) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromMilliseconds(1000.0 / world.TickRate);
        var previousTime = DateTime.UtcNow;

        while (cancellationToken.IsCancellationRequested is false)
        {
            var currentTime = DateTime.UtcNow;
            var deltaTime = (currentTime - previousTime).TotalSeconds;

            previousTime = currentTime;

            await world.TickAsync(deltaTime, cancellationToken).ConfigureAwait(false);

            var sleepTime = delay - (DateTime.UtcNow - currentTime);
            if (sleepTime > TimeSpan.Zero)
            {
                await Task.Delay(sleepTime, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}